﻿/*     This file is part of OAI-PMH .Net.
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Xml.Linq;

namespace FederatedSearch.Controllers
{
    public class oaiCtrlPanelController : ApiController
    {
        /*------------------------------------------------------------------------------------------*/
        /* GET                                                                                        */
        /*------------------------------------------------------------------------------------------*/

        private HttpResponseMessage GetRepositoryList()
        {
            IEnumerable<OAIDataProvider> repositories = OAIOperations.GetRepositoryList();
            if (repositories.Count() > 0)
            {
                return Common.JsonResponse(repositories.Select(dp =>
                    new
                    {
                        dp.AdminEmail,
                        dp.BaseURL,
                        dp.Compression,
                        dp.DeletedRecord,
                        dp.EarliestDatestamp
                        ,
                        dp.Granularity,
                        dp.LastHarvesting,
                        dp.OAIDataProviderId,
                        dp.ProtocolVersion
                        ,
                        dp.RepositoryName
                    }));
            }
            return Common.JsonNullResponse();
        }

        private HttpResponseMessage AddRepository(string baseURL)
        {
            OAIDataProvider dataProvider = OAIOperations.AddRepository(baseURL);
            if (dataProvider != null)
            {
                return Common.JsonResponse(true);
            }
            return Common.JsonNullResponse();
        }

        private HttpResponseMessage HarvestAll(string metadataPrefix)
        {
            Harvester.HarvestAll(metadataPrefix);
            return Common.JsonResponse(true);
        }

        private HttpResponseMessage HarvestRecord(
            string baseURL,
            string identifier,
            string metadataPrefix,
            bool? harvestFiles)
        {
            string oaiIdentifier = Harvester.HarvestRecordAsync(
                baseURL,
                identifier,
                metadataPrefix,
                Enums.DeDuplication.Skip,
                harvestFiles.HasValue ? harvestFiles.Value : false).Result;
            if (!string.IsNullOrEmpty(oaiIdentifier))
            {
                return Common.JsonResponse(oaiIdentifier);
            }
            return Common.JsonNullResponse();
        }

        private HttpResponseMessage GetProperties(string section = null)
        {
            IEnumerable<OAISetting> properties = null;
            if (string.IsNullOrEmpty(section))
            {
                properties = Properties.GetProperties();
            }
            else
            {
                properties = Properties.GetSectionProperties(section);
            }

            if (properties != null && properties.Count() > 0)
            {
                return Common.JsonResponse(properties);
            }
            return Common.JsonNullResponse();
        }

        private HttpResponseMessage AddOrUpdateProperty(string name, string value, string section)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                var setting = new OAISetting() { Key = name, Value = value, Section = section };

                if (OAIOperations.AddOrUpdateSetting(setting))
                {
                    Properties.AddOrUpdate(setting);
                    return Common.JsonResponse(true);
                }

                return Common.JsonErrorResponse("Property hasn't been added or updated.");
            }

            return Common.JsonErrorResponse("Property name can't be null.");
        }

        private HttpResponseMessage DeleteProperty(string name)
        {
            if (!string.IsNullOrEmpty(name) && Properties.CheckForExistance(name))
            {
                if (OAIOperations.DeleteSetting(name) && Properties.Delete(name))
                {
                    return Common.JsonResponse(true);
                }
                else
                {
                    return Common.JsonErrorResponse("Could not delete the property.");
                }
            }

            return Common.JsonErrorResponse("No property with this name could be found.");
        }

        private HttpResponseMessage GetPropertySections()
        {
            Dictionary<string, string> propertySection = Properties.GetPropertySections();

            if (propertySection != null && propertySection.Count() > 0)
            {
                return Common.JsonResponse(propertySection);
            }
            return Common.JsonNullResponse();
        }

        private HttpResponseMessage GetPageFileHarvestProperties()
        {
            var propertySection = Properties.GetPageFileHarvestProperties();

            if (propertySection != null && propertySection.Count > 0)
            {
                return Common.JsonResponse(propertySection);
            }
            return Common.JsonNullResponse();
        }

        public HttpResponseMessage GetResponse(
            string id,
            string baseURL = null,
            string identifier = null,
            string metadataPrefix = null,
            bool? harvestFiles = null,
            string name = null,
            string value = null,
            string section = null)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(id.Trim()))
            {
                return Common.JsonNullResponse();
            }

            switch (id.Trim().ToLower())
            {
                case "getrepositorylist":
                    return GetRepositoryList();
                case "addrepository":
                    return AddRepository(baseURL);
                case "harvestall":
                    return HarvestAll(metadataPrefix);
                case "harvestrecord":
                    return HarvestRecord(baseURL, identifier, metadataPrefix, harvestFiles);
                case "getproperties":
                    return GetProperties(section);
                case "addorupdateproperty":
                    return AddOrUpdateProperty(name, value, section);
                case "deleteproperty":
                    return DeleteProperty(name);
                case "getpropertysections":
                    return GetPropertySections();
                case "getpagefileharvestproperties":
                    return GetPageFileHarvestProperties();
                default:
                    return Common.JsonNullResponse();
            }
        }

        /*------------------------------------------------------------------------------------------*/
        /* POST                                                                                       */
        /*------------------------------------------------------------------------------------------*/

        private HttpResponseMessage BeginHarvesting(IList<DataProviderProperties> dataProviderList)
        {
            if (dataProviderList != null && dataProviderList.Count > 0)
            {
                Harvester.BeginHarvesting(dataProviderList);
                /* get stats */
                return HarvestingStatus();
            }

            return Common.JsonErrorResponse("No data providers were submitted");
        }

        private HttpResponseMessage HarvestingStatus()
        {
            var harvestStats = new List<DataProviderHarvestStats>();
            foreach (var stat in Harvester.HarvestStats)
            {
                harvestStats.Add(stat.Value.HarvestOptions.Stats);
            }

            return Common.JsonResponse(harvestStats);
        }

        private HttpResponseMessage StopHarvesting(int dataProviderId)
        {
            if (dataProviderId > 0 && Harvester.HarvestStats.ContainsKey(dataProviderId))
            {
                if (Harvester.HarvestStats[dataProviderId].TokenSource != null)
                {
                    Harvester.HarvestStats[dataProviderId].TokenSource.Cancel();
                    return Common.JsonResponse(true);
                }
                return Common.JsonErrorResponse("Harvesting is not running.");
            }

            return Common.JsonErrorResponse("No data provider with this ID could be found.");
        }

        private HttpResponseMessage StopAllHarvestingJobs()
        {
            foreach (var stat in Harvester.HarvestStats)
            {
                if (stat.Value.TokenSource != null)
                {
                    stat.Value.TokenSource.Cancel();
                }
            }

            return Common.JsonResponse(true);
        }

        private HttpResponseMessage ClearHarvestingStat(int dataProviderId)
        {
            if (dataProviderId > 0 && Harvester.HarvestStats.ContainsKey(dataProviderId))
            {
                DataProviderIntern tmp;
                if (Harvester.HarvestStats.TryRemove(dataProviderId, out tmp))
                {
                    return Common.JsonResponse(true);
                }
                return Common.JsonErrorResponse("Could not remove data provider stats from list.");
            }

            return Common.JsonErrorResponse("No data provider with this ID could be found.");
        }

        private HttpResponseMessage DeleteMetadata(List<DataProviderProperties> dataProviderList)
        {
            if (dataProviderList != null && dataProviderList.Count > 0)
            {
                if (OAIOperations.DeleteMetadata(dataProviderList))
                {
                    return Common.JsonResponse(true);
                }
                return Common.JsonResponse(false);
            }

            return Common.JsonErrorResponse("No data providers were submitted");
        }

        [HttpPost]
        public HttpResponseMessage PostResponse(
            string id,
            [FromBody] List<DataProviderProperties> dataProviderList,
            int dataProviderId = 0)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(id.Trim()))
            {
                return Common.JsonNullResponse();
            }

            switch (id.Trim().ToLower())
            {
                case "beginharvesting":
                    return BeginHarvesting(dataProviderList);
                case "harvestingstatus":
                    return HarvestingStatus();
                case "stopharvesting":
                    return StopHarvesting(dataProviderId);
                case "stopallharvestingjobs":
                    return StopAllHarvestingJobs();
                case "clearharvestingstat":
                    return ClearHarvestingStat(dataProviderId);
                case "deletemetadata":
                    return DeleteMetadata(dataProviderList);
                default:
                    return Common.JsonNullResponse();
            }
        }
    }
}
