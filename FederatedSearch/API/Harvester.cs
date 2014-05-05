/*     This file is part of OAI-PMH .Net.
*  
*      OAI-PMH .Net is free software: you can redistribute it and/or modify
*      it under the terms of the GNU General Public License as published by
*      the Free Software Foundation, either version 3 of the License, or
*      (at your option) any later version.
*  
*      OAI-PMH .Net is distributed in the hope that it will be useful,
*      but WITHOUT ANY WARRANTY; without even the implied warranty of
*      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*      GNU General Public License for more details.
*  
*      You should have received a copy of the GNU General Public License
*      along with OAI-PMH .Net.  If not, see <http://www.gnu.org/licenses/>.
*----------------------------------------------------------------------------*/

using FederatedSearch.API.Common;
using FederatedSearch.API.Internal;
using FederatedSearch.API.MdFormats;
using FederatedSearch.Models;
using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Net;

namespace FederatedSearch.API
{
    public class Harvester
    {
        public static ConcurrentDictionary<int, DataProviderIntern> HarvestStats = new ConcurrentDictionary<int, DataProviderIntern>();

        #region Helper function

        public static XDocument GetAndParseXML(string url)
        {
            string body;
            using (HttpClient client = new HttpClient())
            {
                body = client.GetStringAsync(url).Result;
            }

            return XDocument.Parse(body, LoadOptions.None);
        }

        public static async Task<XDocument> GetAndParseXMLAsync(string url)
        {
            string body = string.Empty;
            var handler = new HttpClientHandler();
            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip |
                                                 DecompressionMethods.Deflate;
            }
            using (var client = new HttpClient(handler))
            {
                body = await client.GetStringAsync(url).ConfigureAwait(false);
            }
            return XDocument.Parse(body);
        }

        public static RecordQueryResult ParseRecord(XElement record, string metadataPrefix)
        {
            return new RecordQueryResult(
                MlDecode.Header(record.Element(MlNamespaces.oaiNs + "header")),
                MlDecode.Metadata(record.Element(MlNamespaces.oaiNs + "metadata"), metadataPrefix),
                Properties.harvestAbout ?
                    MlDecode.MetaList(record.Elements(MlNamespaces.oaiNs + "about").ToList()) :
                    null);
        }
        public static async Task<RecordQueryResult> ParseRecordAsync(XElement record, string metadataPrefix)
        {
            Task<Header> hd = MlDecode.HeaderAsync(record.Element(MlNamespaces.oaiNs + "header"));
            Task<Metadata> md = MlDecode.MetadataAsync(record.Element(MlNamespaces.oaiNs + "metadata"), metadataPrefix);
            if (Properties.harvestAbout)
            {
                Task<IEnumerable<Metadata>> about = MlDecode.MetaListAsync(record.Elements(MlNamespaces.oaiNs + "about").ToList());
                return new RecordQueryResult(
                    await hd.ConfigureAwait(false),
                    await md.ConfigureAwait(false),
                    await about.ConfigureAwait(false));
            }
            else
            {
                return new RecordQueryResult(
                    await hd.ConfigureAwait(false),
                    await md.ConfigureAwait(false));
            }
        }

        public static void SaveXMLWithErrors(XDocument xd, DbEntityValidationException dbEx, string repositoryName)
        {
            /* build new name that doesn't exist */
            string path = Directory.GetCurrentDirectory() + "\\errorFiles\\";
            string fileName = repositoryName.Trim().Replace(' ', '-') + "_" + DateTime.UtcNow.ToString(Enums.Granularity.Date);
            int count = 1;
            string file = string.Empty;
            do
            {
                file = string.Format("{0}{1}_{2}.{3}", path, fileName, count, "xml");
                count += 1;
            } while (File.Exists(file));

            /* add error description(s) */
            foreach (var validationErrors in dbEx.EntityValidationErrors)
            {
                foreach (var validationError in validationErrors.ValidationErrors)
                {
                    xd.Root.Add(new XElement(MlNamespaces.oaiNs + "error",
                        new XElement(MlNamespaces.oaiNs + "class", validationErrors.Entry.Entity.GetType().FullName),
                        new XElement(MlNamespaces.oaiNs + "property", validationError.PropertyName),
                        new XElement(MlNamespaces.oaiNs + "message", validationError.ErrorMessage)));
                }
            }

            /* try to save */
            try
            {
                xd.Save(file);
            }
            catch (Exception e)
            {
                /* for debugging purpose only */
                string msg = e.Message;
            }
        }

        #endregion /* Helper functions */

        public static async Task<string> HarvestRecordAsync(
            string baseURL,
            string identifier,
            string metadataPrefix,
            Enums.DeDuplication deDup,
            bool harvestFile)
        {
            if (string.IsNullOrEmpty(metadataPrefix))
            {
                metadataPrefix = "oai_dc";
            }
            string url = baseURL + "?verb=GetRecord&identifier=" + identifier + "&metadataPrefix=" + metadataPrefix;
            OAIDataProvider dataProvider;
            using (var context = new OaiPmhContext())
            {
                dataProvider = context.OAIDataProvider.FirstOrDefault(dp => dp.BaseURL == baseURL);
            }
            if (dataProvider != null)
            {
                return await HarvestRecordsAsync(
                    dataProvider,
                    url,
                    metadataPrefix,
                    deDup,
                    false,
                    harvestFile,
                    false).ConfigureAwait(false);
            }
            return null;
        }

        public static void HarvestAll(string metadataPrefix)
        {
            var harvestSettingsList = new List<DataProviderProperties>();
            using (var context = new OaiPmhContext())
            {
                foreach (var dataProvider in context.OAIDataProvider.ToList())
                {
                    harvestSettingsList.Add(new DataProviderProperties()
                    {
                        BaseURL = dataProvider.BaseURL,
                        Exclude = false,
                        FullHarvestDelete = false,
                        HarvestDeleteFiles = true,
                        MetadataPrefix = string.IsNullOrEmpty(metadataPrefix) ? null : metadataPrefix,
                        OAIDataProviderId = dataProvider.OAIDataProviderId,
                        RepositoryName = dataProvider.RepositoryName
                    });
                }
            }
            BeginHarvesting(harvestSettingsList);
        }

        public static void BeginHarvesting(IList<DataProviderProperties> dataProviderList)
        {
            /* list of ids from data providers that were not excluded and are not currently being harvested */
            var useOnlyDataProviders = dataProviderList
                .Where(dp =>
                    !dp.Exclude &&
                    !HarvestStats.ContainsKey(dp.OAIDataProviderId))
                .Select(dp => dp.OAIDataProviderId);

            var dataProviders = new List<OAIDataProvider>();
            using (var context = new OaiPmhContext())
            {
                dataProviders = (from dp in context.OAIDataProvider
                                 join uo in useOnlyDataProviders on dp.OAIDataProviderId equals uo
                                 select dp).ToList();
            }

            /* add new statistical objects for data providers and start jobs */
            foreach (var id in useOnlyDataProviders)
            {
                var dataProvider = dataProviders.Where(dp => dp.OAIDataProviderId == id).FirstOrDefault();
                var harvestSettings = dataProviderList.Where(dp => dp.OAIDataProviderId == id).FirstOrDefault();

                if (dataProvider != null && harvestSettings != null)
                {
                    harvestSettings.MetadataPrefix = string.IsNullOrEmpty(harvestSettings.MetadataPrefix) ?
                        "oai_dc" : harvestSettings.MetadataPrefix;
                    harvestSettings.Stats = new DataProviderHarvestStats() { OAIDataProviderId = id, Status = "Starting" };
                    var tmpIntern = new DataProviderIntern()
                                        {
                                            DataProvider = dataProvider,
                                            HarvestOptions = harvestSettings,
                                            TokenSource = new CancellationTokenSource()
                                        };
                    if (HarvestStats.TryAdd(id, tmpIntern))
                    {
                        Task.Factory.StartNew(
                            delegate { StartHarvestTask(id); },
                            tmpIntern.TokenSource.Token,
                            TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                            TaskScheduler.Default);
                    }
                }
            }
        }

        private static void StartHarvestTask(int hsId)
        {
            string url = HarvestStats[hsId].DataProvider.BaseURL +
                    "?verb=ListRecords&metadataPrefix=" + HarvestStats[hsId].HarvestOptions.MetadataPrefix;

            if (!HarvestStats[hsId].HarvestOptions.FullHarvestDelete)
            {
                var fromDate = Common.MlDecode.SafeDateTime(HarvestStats[hsId].HarvestOptions.From);
                var untilDate = Common.MlDecode.SafeDateTime(HarvestStats[hsId].HarvestOptions.Until);
                if (fromDate != null && untilDate != null)
                {
                    url += "&from=" + HarvestStats[hsId].HarvestOptions.From +
                        "&until=" + HarvestStats[hsId].HarvestOptions.Until;
                }
                else if (fromDate != null)
                {
                    url += "&from=" + HarvestStats[hsId].HarvestOptions.From;
                }
                else if (untilDate != null)
                {
                    url += "&until=" + HarvestStats[hsId].HarvestOptions.Until;
                }
                else if (HarvestStats[hsId].DataProvider.LastHarvesting.HasValue)
                {
                    url += "&from=" +
                        ((HarvestStats[hsId].DataProvider.Granularity.Length == 10) ?
                            HarvestStats[hsId].DataProvider.LastHarvesting.Value.ToString(Enums.Granularity.Date)
                            : HarvestStats[hsId].DataProvider.LastHarvesting.Value.ToString(Enums.Granularity.DateTime));
                }
            }

            var sw = new Stopwatch();
            string resumptionToken = string.Empty;
            int retryCount = Properties.retryRetrievalCount;
            do
            {
                /* check if enough time has passed before we send a new request - don't want to make DoS attack */
                if (sw.IsRunning && sw.Elapsed < Properties.minTimeBetweenRequests)
                {
                    var delayTime = Properties.minTimeBetweenRequests - sw.Elapsed;
                    if (delayTime > TimeSpan.Zero)
                    {
                        Task.Delay(delayTime).Wait();
                    }
                }

                sw.Restart();

                if (!HarvestStats.ContainsKey(hsId) || HarvestStats[hsId].TokenSource.IsCancellationRequested)
                {
                    HarvestStats[hsId].HarvestOptions.Stats.Status = "Cancelled";
                    HarvestStats[hsId].HarvestOptions.Stats.Message = "User cancelled harvest job";
                    break;
                }

                HarvestStats[hsId].HarvestOptions.Stats.Status = "Harvesting";

                resumptionToken = HarvestRecordsAsync(
                                      HarvestStats[hsId].DataProvider,
                                      url,
                                      HarvestStats[hsId].HarvestOptions.MetadataPrefix,
                                      Enums.DeDuplication.Skip,
                                      true,
                                      HarvestStats[hsId].HarvestOptions.HarvestDeleteFiles,
                                      true).Result;

                if (resumptionToken != "retry")
                {
                    url = HarvestStats[hsId].DataProvider.BaseURL + "?verb=ListRecords&resumptionToken=" + resumptionToken;
                    retryCount = Properties.retryRetrievalCount;
                }
                else
                {
                    if (retryCount <= 0)
                    {
                        break;
                    }
                    retryCount -= 1;
                }
            }
            while (!string.IsNullOrEmpty(resumptionToken));
            sw.Stop();
            HarvestStats[hsId].HarvestOptions.Stats.Status = "Completed";
        }

        private static async Task<string> HarvestRecordsAsync(
            OAIDataProvider dataProvider,
            string url,
            string metadataPrefix,
            Enums.DeDuplication deDup,
            bool updateStats,
            bool harvestFiles,
            bool isList,
            int retryCount = 3)
        {
            if (dataProvider == null)
            {
                if (updateStats)
                {
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Status = "Exception";
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Message = "Data provider is not initialized";
                }
                return null;
            }
            if (string.IsNullOrEmpty(metadataPrefix))
            {
                if (updateStats)
                {
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Status = "Exception";
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Message = "Metadata format is not provided";
                }
                return null;
            }

            try
            {
                XDocument xd;
                try
                {
                    xd = await GetAndParseXMLAsync(url).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Status = "Retrying";
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Message = e.Message;
                    return "retry";
                }

                /* we validate if it's enabled */
                bool errors = false;
                if (Properties.validateXml)
                {
                    List<string> eMsgs = new List<string>();
                    xd.Validate(Properties.schemas, (o, e) => { errors = true; eMsgs.Add(e.Message); });
                }

                if (Properties.validateXml ? !errors : true)
                {
                    /* get harvest date */
                    DateTime harvestDate;
                    bool isHarvestDateTime;
                    MlDecode.ResponseDate(ref xd, out harvestDate, out isHarvestDateTime);

                    XElement listRecords = isList ? xd.Root.Element(MlNamespaces.oaiNs + "ListRecords") :
                                                    xd.Root.Element(MlNamespaces.oaiNs + "GetRecord");
                    if (listRecords != null)
                    {
                        /* parse records */
                        List<RecordQueryResult> records = new List<RecordQueryResult>();
                        foreach (var record in listRecords.Elements(MlNamespaces.oaiNs + "record"))
                        {
                            records.Add(ParseRecordAsync(record, metadataPrefix).Result);
                        }
                        int itemsPerPage = records.Count;

                        if (records.Count > 0)
                        {
                            using (var context = new OaiPmhContext())
                            {
                                /* try to deduplicate (if selected) and add records to database */
                                try
                                {
                                    /* update timestamp of last harvesting */
                                    context.OAIDataProvider.Attach(dataProvider);
                                    dataProvider.LastHarvesting = harvestDate;
                                    context.Entry(dataProvider).State = EntityState.Modified;

                                    DeDuplicate.Records(
                                        records,
                                        context,
                                        deDup);

                                    /* add records to database */
                                    foreach (var record in records)
                                    {
                                        if (harvestFiles)
                                        {
                                            FileHarvester.GetFile(dataProvider.BaseURL, record);
                                        }
                                        RecordQueryResult.AddRecordToDatabase(
                                            record,
                                            context,
                                            dataProvider,
                                            metadataPrefix,
                                            harvestDate,
                                            Properties.addProvenanceToHarvestedRecords,
                                            Properties.createNewIdentifierForHarvestedRecords,
                                            Properties.identifierBase,
                                            isHarvestDateTime);
                                    }
                                    context.SaveChanges();
                                }
                                catch (DbEntityValidationException dbEx)
                                {
                                    if (updateStats)
                                    {
                                        HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Status = "Exception";
                                        HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Message = "Database exception occured. Please contact administrator";
                                    }
                                    SaveXMLWithErrors(xd, dbEx, dataProvider.RepositoryName);
                                }
                                catch (Exception e)
                                {
                                    if (updateStats)
                                    {
                                        HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Status = "Exception";
                                        HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Message = e.Message;
                                    }
                                    /* for debugging purpose only */
                                    string msg = e.Message;
                                }
                            }
                            if (!isList)
                            {
                                return records.Count > 0 ? records[0].Header.OAI_Identifier : null;
                            }
                        }

                        var resumption = listRecords.Element(MlNamespaces.oaiNs + "resumptionToken");
                        if (resumption != null)
                        {
                            /* set complete list size and current progress */
                            if (updateStats)
                            {
                                int completeListSize = 0;
                                int cursor = 0;

                                var listSizeAttribute = resumption.Attribute("completeListSize");
                                var cursorAttribute = resumption.Attribute("cursor");

                                if ((listSizeAttribute != null &&
                                     int.TryParse(listSizeAttribute.Value, out completeListSize)) &&
                                    (cursorAttribute != null &&
                                    int.TryParse(cursorAttribute.Value, out cursor)))
                                {
                                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.RatioAll = completeListSize;
                                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.RatioDone = cursor + itemsPerPage;
                                }
                            }
                            if (!String.IsNullOrEmpty(resumption.Value))
                            {
                                return resumption.Value;
                            }
                        }
                    }
                }
                else if (updateStats)
                {
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Status = "Exception";
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Message = "Validation has failed";
                }
            }
            catch (Exception e)
            {
                if (updateStats)
                {
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Status = "Exception";
                    HarvestStats[dataProvider.OAIDataProviderId].HarvestOptions.Stats.Message = e.Message;
                }
                /* for debugging purpose only */
                string msg = e.Message;
            }

            return null;
        }
    }
}
