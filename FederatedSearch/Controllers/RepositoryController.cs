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

using FederatedSearch.API;
using FederatedSearch.API.Common;
using FederatedSearch.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FederatedSearch.Controllers
{
    public class RepositoryController : Controller
    {
        private string ok = "ok";
        private string failure = "failure";


        #region DataProvider

        public async Task<ActionResult> DataProvider(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                switch (id.Trim().ToLower())
                {
                    case "list":
                        var baseLocalUrl = Common.GetBaseUrl(this);
                        var repositoryList = await OaiApiRestService.GetDataProviders(baseLocalUrl) ?? new List<OAIDataProvider>();
                        return View("DataProviderList", repositoryList);
                    case "addrepository":
                        return View("DataProviderAdd");
                    default: return null;
                }
            }
            return null;
        }

        [HttpPost]
        public async Task<ActionResult> DataProvider(string id, string baseURL = null)
        {
            if (!string.IsNullOrEmpty(id))
            {
                switch (id.Trim().ToLower())
                {
                    case "addrepository":
                        var baseLocalUrl = Common.GetBaseUrl(this);
                        baseURL = baseURL.Trim();
                        if (await OaiApiRestService.AddDataProvider(baseLocalUrl, baseURL))
                        {
                            return Json(new { successfull = true });
                        }
                        else
                        {
                            return Json(new { successfull = false });
                        }

                    default: return Json(new { successfull = false });
                }
            }
            return Json(new { successfull = false });
        }

        #endregion /* DataProvider */

        #region Harvet

        public async Task<ActionResult> Harvest(string id)
        {
            var baseLocalUrl = Common.GetBaseUrl(this);
            var repositoryList = await OaiApiRestService.GetDataProviders(baseLocalUrl);

            if (!string.IsNullOrEmpty(id))
            {
                switch (id.Trim().ToLower())
                {
                    case "record":
                        var selectList = new List<SelectListItem>();
                        if (repositoryList != null)
                        {
                            selectList.Add(new SelectListItem() { Value = "", Text = "Select Dataprovider" });
                            foreach (var dp in repositoryList)
                            {
                                selectList.Add(new SelectListItem() { Value = dp.BaseURL, Text = dp.RepositoryName });
                            }
                        }
                        return View("HarvestRecord", selectList);
                    case "list":
                    default:
                        break;
                }
            }

            var settings = RemodelDataProvidersToProperties(repositoryList).ToList();
            var harvestStats = await OaiApiRestService.HarvestingStats(baseLocalUrl);
            foreach (var stat in harvestStats)
            {
                var tmp = settings.FirstOrDefault(s => s.OAIDataProviderId == stat.OAIDataProviderId);
                if (tmp != null)
                {
                    tmp.Stats = stat;
                }
            }
            return View("Harvest", settings);
        }

        [HttpPost]
        public async Task<ActionResult> Harvest(
            string id,
            int dataProviderId = 0,
            List<DataProviderProperties> repositoryList = null,
            string baseURL = null,
            string identifier = null,
            string metadataPrefix = null,
            bool? harvestFiles = null)
        {
            var baseLocalUrl = Common.GetBaseUrl(this);
            switch (id.Trim().ToLower())
            {
                case "start":
                    if (repositoryList != null && repositoryList.Count > 0)
                    {
                        var startResult = await OaiApiRestService.BeginHarvesting(baseLocalUrl, repositoryList);
                        string startStatus = (startResult != null && startResult.Count > 0) ? ok : failure;
                        return Json(new { status = startStatus, dataProviderId = repositoryList[0].OAIDataProviderId, result = startResult });
                    }
                    return Json(new { status = failure, result = "No data providers were submitted!" });

                case "stop":
                    bool stopResult = await OaiApiRestService.StopHarvesting(baseLocalUrl, dataProviderId);
                    string stopStatus = stopResult ? ok : failure;
                    return Json(new { status = stopStatus, dataProviderId = dataProviderId });

                case "status":
                    var statusResult = await OaiApiRestService.HarvestingStats(baseLocalUrl);
                    string status = (statusResult != null && statusResult.Count > 0) ? ok : failure;
                    return Json(new { status = status, result = statusResult });

                case "clear":
                    bool clearResult = await OaiApiRestService.ClearHarvestingStat(baseLocalUrl, dataProviderId);
                    string clearStatus = clearResult ? ok : failure;
                    return Json(new { status = clearStatus, dataProviderId = dataProviderId });

                case "startall":
                    if (repositoryList != null && repositoryList.Count > 0)
                    {
                        var startResult = await OaiApiRestService.BeginHarvesting(baseLocalUrl, repositoryList);
                        string startStatus = (startResult != null && startResult.Count > 0) ? ok : failure;
                        return Json(new { status = startStatus, operation = "startAll", result = startResult });
                    }
                    return Json(new { status = failure, result = "No data providers were submitted!" });

                case "stopall":
                    bool stopAllResult = await OaiApiRestService.StopHarvestingAll(baseLocalUrl);
                    string stopAllStatus = stopAllResult ? ok : failure;
                    return Json(new { status = stopAllStatus, operation = "stopAll" });

                case "record":
                    string oaiIdentifier = await OaiApiRestService.HarvestRecord(baseLocalUrl, baseURL, identifier, metadataPrefix, harvestFiles);
                    if (!string.IsNullOrEmpty(oaiIdentifier))
                    {
                        return Json(new { successfull = true, identifier = oaiIdentifier });
                    }
                    return Json(new { successfull = false });

                default:
                    return Json(new { status = failure });
            }
        }

        #endregion /* Harvest */

        #region FileHarvest

        public async Task<ActionResult> PageFileHarvestProperties()
        {
            var baseLocalUrl = Common.GetBaseUrl(this);
            var pageFileHarvestProps = await OaiApiRestService.GetPageFileHarvestProperties(baseLocalUrl);

            return View("PageFileHarvestProperties", pageFileHarvestProps);
        }

        [HttpPost]
        public async Task<ActionResult> PageFileHarvestProperties(
            string id,
            PageFileHarvestProperties options = null,
            string baseUri = null,
            string firstHttpMethod = null,
            string secondHttpMethod = null,
            string lineRegex = null,
            string valueRegex = null,
            string secondTierValueOption = null)
        {
            var baseLocalUrl = Common.GetBaseUrl(this);
            switch (id.Trim().ToLower())
            {
                case "addorupdate":
                    var jsonString = JsonConvert.SerializeObject(new PageFileHarvestProperties()
                    {
                        BaseUri = baseUri,
                        FirstHttpMethod = firstHttpMethod,
                        SecondHttpMethod = secondHttpMethod,
                        LineRegex = lineRegex,
                        ValueRegex = valueRegex,
                        SecondTierValueOption = secondTierValueOption
                    });

                    if (await OaiApiRestService.AddOrUpdateProperty(baseLocalUrl, baseUri.Trim(), jsonString, "pfhp"))
                    {
                        return Json(new
                        {
                            status = ok,
                            baseUri = baseUri,
                            firstHttpMethod = firstHttpMethod,
                            secondHttpMethod = secondHttpMethod,
                            lineRegex = lineRegex,
                            valueRegex = valueRegex,
                            secondTierValueOption = secondTierValueOption
                        });
                    }
                    return Json(new { status = failure });

                case "delete":
                    if (await OaiApiRestService.DeleteProperty(baseLocalUrl, baseUri))
                    {
                        return Json(new { status = ok, baseUri = baseUri });
                    }
                    return Json(new { status = failure });
            }

            return null;
        }

        #endregion /* FileHarvest */

        #region DeleteMetadata

        public async Task<ActionResult> DeleteMetadata()
        {
            var baseLocalUrl = Common.GetBaseUrl(this);
            var repositoryList = await OaiApiRestService.GetDataProviders(baseLocalUrl);

            var settings = RemodelDataProvidersToProperties(repositoryList).ToList();
            return View("DeleteMetadata", settings);
        }

        [HttpPost]
        public async Task<ActionResult> DeleteMetadata(
            List<DataProviderProperties> repositoryList = null)
        {
            var baseLocalUrl = Common.GetBaseUrl(this);
            if (repositoryList != null && repositoryList.Count > 0)
            {
                bool deleteResult = await OaiApiRestService.DeleteMetadata(baseLocalUrl, repositoryList);
                string deleteStatus = deleteResult ? ok : failure;
                return Json(new { status = deleteStatus, dataProviderId = repositoryList[0].OAIDataProviderId, result = deleteResult });
            }
            return Json(new { status = failure, result = "No data providers were submitted!" });
        }

        #endregion /* DeleteMetadata */

        #region Properties

        public async Task<ActionResult> Properties()
        {
            var baseLocalUrl = Common.GetBaseUrl(this);
            var sectionList = await OaiApiRestService.GetPropertySections(baseLocalUrl);
            var propertyList = await OaiApiRestService.GetProperties(baseLocalUrl, null);

            Dictionary<string, List<OAISetting>> propertyGroups;

            if (sectionList != null && sectionList.Count > 0)
            {
                ViewBag.SectionList = sectionList;
                propertyGroups = sectionList.GroupJoin(
                    propertyList,
                    s => s.Key,
                    p => p.Section,
                    (s, group) => new KeyValuePair<string, List<OAISetting>>(s.Key, group.ToList())).ToDictionary(k => k.Key, v => v.Value);

            }
            else
            {
                var tmpPropsList = new List<OAISetting>();
                tmpPropsList.Add(new OAISetting() { Key = "PropertySections", Value = "", Section = "hp" });
                propertyGroups = new Dictionary<string, List<OAISetting>>();
                propertyGroups.Add("Harvester", tmpPropsList);
            }

            return View("Properties", propertyGroups);
        }

        [HttpPost]
        public async Task<ActionResult> Properties(
            string id,
            string name = null,
            string value = null,
            string section = null,
            string oldSection = null)
        {
            var baseLocalUrl = Common.GetBaseUrl(this);
            switch (id.Trim().ToLower())
            {
                case "addorupdate":
                    if (await OaiApiRestService.AddOrUpdateProperty(baseLocalUrl, name.Trim(), value.Trim(), section.Trim()))
                    {
                        return Json(new { status = ok, name = name, value = value, section = section, oldSection = oldSection });
                    }
                    return Json(new { status = failure });

                case "delete":
                    if (await OaiApiRestService.DeleteProperty(baseLocalUrl, name.Trim()))
                    {
                        return Json(new { status = ok, name = name, section = section });
                    }
                    return Json(new { status = failure });
            }

            return null;
        }

        #endregion /* Properties */


        private IEnumerable<DataProviderProperties> RemodelDataProvidersToProperties(IEnumerable<OAIDataProvider> dataProviders)
        {
            if (dataProviders == null)
            {
                yield break;
            }

            foreach (var dp in dataProviders)
            {
                yield return new DataProviderProperties()
                {
                    BaseURL = dp.BaseURL,
                    Exclude = false,
                    FullHarvestDelete = false,
                    HarvestDeleteFiles = false,
                    OAIDataProviderId = dp.OAIDataProviderId,
                    RepositoryName = dp.RepositoryName
                };
            }
        }
    }
}
