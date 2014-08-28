/*     This file is part of OAI-PMH-.Net.
*  
*      OAI-PMH-.Net is free software: you can redistribute it and/or modify
*      it under the terms of the GNU General Public License as published by
*      the Free Software Foundation, either version 3 of the License, or
*      (at your option) any later version.
*  
*      OAI-PMH-.Net is distributed in the hope that it will be useful,
*      but WITHOUT ANY WARRANTY; without even the implied warranty of
*      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*      GNU General Public License for more details.
*  
*      You should have received a copy of the GNU General Public License
*      along with OAI-PMH-.Net.  If not, see <http://www.gnu.org/licenses/>.
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
                    case "add":
                        return View("DataProviderAdd");
                    case "list":
                    default:
                        var baseLocalUrl = Common.GetBaseApiUrl(this);
                        var repositoryList = await OaiApiRestService.GetDataProviders(baseLocalUrl) ?? new List<OAIDataProvider>();
                        return View("DataProviderList", repositoryList);
                }
            }
            return null;
        }

        [HttpPost]
        public async Task<ActionResult> DataProvider(
            string id,
            string baseURL = null,
            OAIDataProvider dataProvider = null,
            string OAIDataProviderId = null)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var baseLocalUrl = Common.GetBaseApiUrl(this);
                switch (id.Trim().ToLower())
                {
                    case "addorupdate":
                        baseURL = string.IsNullOrEmpty(baseURL) ? null : baseURL.Trim();
                        var jsonString = JsonConvert.SerializeObject(dataProvider);
                        dataProvider = await OaiApiRestService.AddOrUpdateDataProvider(baseLocalUrl, baseURL, jsonString);
                        if (dataProvider != null)
                        {
                            return Json(new
                            {
                                status = ok,
                                dataProvider = dataProvider
                            });
                        }
                        return Json(new { status = failure });

                    case "delete":
                        if (await OaiApiRestService.DeleteDataProvider(baseLocalUrl, OAIDataProviderId))
                        {
                            return Json(new { status = ok, OAIDataProviderId = OAIDataProviderId });
                        }
                        return Json(new { status = failure });

                    case "reidentify":
                        dataProvider = await OaiApiRestService.ReIdentifyDataProvider(baseLocalUrl, OAIDataProviderId);
                        if (dataProvider != null)
                        {
                            return Json(new { status = ok, dataProvider = dataProvider });
                        }
                        return Json(new { status = failure });
                }
            }

            return Json(new { status = failure });
        }

        #endregion /* DataProvider */

        #region Harvest

        public async Task<ActionResult> Harvest(string id)
        {
            var baseLocalUrl = Common.GetBaseApiUrl(this);
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
            var harvestStats = await OaiApiRestService.HarvestingStats(baseLocalUrl) ?? new List<DataProviderHarvestStats>();
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
            if (!string.IsNullOrEmpty(id))
            {
                var baseLocalUrl = Common.GetBaseApiUrl(this);
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
                        string oaiHeaderId = await OaiApiRestService.HarvestRecord(baseLocalUrl, baseURL, identifier, metadataPrefix, harvestFiles);
                        if (!string.IsNullOrEmpty(oaiHeaderId))
                        {
                            return Json(new { successfull = true, identifier = oaiHeaderId });
                        }
                        return Json(new { successfull = false });
                }
            }

            return Json(new { status = failure });
        }

        #endregion /* Harvest */

        #region FileHarvest

        public async Task<ActionResult> PageFileHarvestProperties()
        {
            var baseLocalUrl = Common.GetBaseApiUrl(this);
            var pageFileHarvestProps = await OaiApiRestService.GetPageFileHarvestProperties(baseLocalUrl);

            return View("PageFileHarvestProperties", pageFileHarvestProps);
        }

        [HttpPost]
        public async Task<ActionResult> PageFileHarvestProperties(
            string id,
            PageFileHarvestProperties properties = null,
            string BaseUri = null)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var baseLocalUrl = Common.GetBaseApiUrl(this);
                switch (id.Trim().ToLower())
                {
                    case "addorupdate":
                        var jsonString = JsonConvert.SerializeObject(properties);
                        if (await OaiApiRestService.AddOrUpdateProperty(baseLocalUrl, properties.BaseUri, jsonString, "pfhp"))
                        {
                            return Json(new
                            {
                                status = ok,
                                properties = properties
                            });
                        }
                        return Json(new { status = failure });

                    case "delete":
                        if (await OaiApiRestService.DeleteProperty(baseLocalUrl, BaseUri))
                        {
                            return Json(new { status = ok, BaseUri = BaseUri });
                        }
                        return Json(new { status = failure });
                }
            }

            return Json(new { status = failure });
        }

        #endregion /* FileHarvest */

        #region DeleteMetadata

        public async Task<ActionResult> DeleteMetadata()
        {
            var baseLocalUrl = Common.GetBaseApiUrl(this);
            var repositoryList = await OaiApiRestService.GetDataProviders(baseLocalUrl);

            var settings = RemodelDataProvidersToProperties(repositoryList).ToList();
            return View("DeleteMetadata", settings);
        }

        [HttpPost]
        public async Task<ActionResult> DeleteMetadata(
            List<DataProviderProperties> repositoryList = null)
        {
            var baseLocalUrl = Common.GetBaseApiUrl(this);
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
            var baseLocalUrl = Common.GetBaseApiUrl(this);
            var sectionList = await OaiApiRestService.GetPropertySections(baseLocalUrl);
            var propertyList = await OaiApiRestService.GetProperties(baseLocalUrl, null);

            Dictionary<string, List<Property>> propertyGroups;

            if (sectionList != null && sectionList.Count > 0)
            {
                ViewBag.SectionList = sectionList;
                propertyGroups = sectionList.GroupJoin(
                    propertyList,
                    s => s.Key,
                    p => p.Section,
                    (s, group) => new KeyValuePair<string, List<Property>>(s.Key, group.ToList())).ToDictionary(k => k.Key, v => v.Value);

            }
            else
            {
                var tmpPropsList = new List<Property>();
                tmpPropsList.Add(new Property() { Key = "PropertySections", Value = "hp=Harvester;", Section = "hp" });
                propertyGroups = new Dictionary<string, List<Property>>();
                propertyGroups.Add("hp", tmpPropsList);
                sectionList = new Dictionary<string, string>();
                sectionList.Add("hp", "Harvester");
                ViewBag.SectionList = sectionList;
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
            if (!string.IsNullOrEmpty(id))
            {
                var baseLocalUrl = Common.GetBaseApiUrl(this);
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
            }

            return Json(new { status = failure });
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
