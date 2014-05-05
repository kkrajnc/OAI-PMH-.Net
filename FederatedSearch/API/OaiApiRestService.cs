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

using FederatedSearch.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FederatedSearch.API
{
    public static class OaiApiRestService
    {
        /* GET                                                                                        */
        /* -----------------------------------------------------------------------------------------*/
        private static async Task<T> GetRequest<T>(string uri)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    return JsonConvert.DeserializeObject<T>(
                        await httpClient.GetStringAsync(uri));
                }
            }
            catch (HttpRequestException)
            {
                return default(T);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        internal static async Task<MetaList> GetMetadataAsync(string baseLocalUrl, int ipp = 0, string search = null, int page = 0)
        {
            return await GetRequest<MetaList>(baseLocalUrl + "api/oaiMetadata"
                                                      + "?ipp=" + (ipp <= 0 ? 10 : ipp)
                                                      + (string.IsNullOrEmpty(search) ? "" : ("&search=" + search))
                                                      + (page <= 1 ? "" : ("&page=" + page)));
        }

        internal static async Task<Metadata> GetMetadataAsync(string baseLocalUrl, string id)
        {
            return await GetRequest<Metadata>(baseLocalUrl + "api/oaiMetadata?id=" + id);
        }

        internal static async Task<bool> AddDataProvider(string baseLocalUrl, string baseURL)
        {
            return await GetRequest<bool>(baseLocalUrl + "api/oaiCtrlPanel/addRepository?baseURL=" + baseURL);
        }

        internal static async Task<IEnumerable<OAIDataProvider>> GetDataProviders(string baseLocalUrl)
        {
            return await GetRequest<IEnumerable<OAIDataProvider>>(baseLocalUrl + "api/oaiCtrlPanel/getRepositoryList");
        }

        internal static async Task<string> HarvestRecord(
            string baseLocalUrl, 
            string baseURL,
            string identifier,
            string metadataPrefix = null,
            bool? harvestFiles = null)
        {
            var sb = new StringBuilder();
            sb.Append(baseLocalUrl + "api/oaiCtrlPanel/harvestRecord?" +
                "baseURL=" + baseURL +
                "&identifier=" + identifier +
                "&metadataPrefix=" + metadataPrefix);

            if (harvestFiles != null && harvestFiles.HasValue)
            {
                sb.Append("&harvestFiles=" + harvestFiles.Value);
            }
            return await GetRequest<string>(sb.ToString());
        }

        internal static async Task<IEnumerable<OAISetting>> GetProperties(string baseLocalUrl, string section)
        {
            return await GetRequest<IEnumerable<OAISetting>>(baseLocalUrl + "api/oaiCtrlPanel/getProperties?section=" + section);
        }

        internal static async Task<bool> AddOrUpdateProperty(string baseLocalUrl, string name, string value, string section)
        {
            return await GetRequest<bool>(baseLocalUrl + "api/oaiCtrlPanel/addOrUpdateProperty?" + 
                "name=" + name + "&value=" + value + "&section=" + section);
        }

        internal static async Task<bool> DeleteProperty(string baseLocalUrl, string name)
        {
            return await GetRequest<bool>(baseLocalUrl + "api/oaiCtrlPanel/deleteProperty?name=" + name);
        }

        internal static async Task<Dictionary<string, string>> GetPropertySections(string baseLocalUrl)
        {
            return await GetRequest<Dictionary<string, string>>(baseLocalUrl + "api/oaiCtrlPanel/getPropertySections");
        }

        internal static async Task<List<PageFileHarvestProperties>> GetPageFileHarvestProperties(string baseLocalUrl)
        {
            return await GetRequest<List<PageFileHarvestProperties>>(baseLocalUrl + "api/oaiCtrlPanel/getPageFileHarvestProperties");
        }


        /* POST                                                                                       */
        /* -----------------------------------------------------------------------------------------*/
        private static async Task<T> PostRequest<T>(object value, string uri)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var jsonStr = JsonConvert.SerializeObject(value);
                    var result = await httpClient.PostAsync(
                                    uri,
                                    new StringContent(jsonStr, Encoding.UTF8, "application/json"));

                    return JsonConvert.DeserializeObject<T>(
                        await result.Content.ReadAsStringAsync());
                }
            }
            catch (HttpRequestException)
            {
                return default(T);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        internal static async Task<List<DataProviderHarvestStats>> BeginHarvesting(string baseLocalUrl, List<DataProviderProperties> repositoryList)
        {
            return await PostRequest<List<DataProviderHarvestStats>>(repositoryList, baseLocalUrl + "api/oaiCtrlPanel/BeginHarvesting");
        }

        internal static async Task<List<DataProviderHarvestStats>> HarvestingStats(string baseLocalUrl)
        {
            return await PostRequest<List<DataProviderHarvestStats>>(null, baseLocalUrl + "api/oaiCtrlPanel/HarvestingStatus");
        }

        internal static async Task<bool> StopHarvesting(string baseLocalUrl, int dataProviderId)
        {
            return await PostRequest<bool>(null, baseLocalUrl + "api/oaiCtrlPanel/StopHarvesting?dataProviderId=" + dataProviderId);
        }

        internal static async Task<bool> ClearHarvestingStat(string baseLocalUrl, int dataProviderId)
        {
            return await PostRequest<bool>(null, baseLocalUrl + "api/oaiCtrlPanel/ClearHarvestingStat?dataProviderId=" + dataProviderId);
        }

        internal static async Task<bool> StopHarvestingAll(string baseLocalUrl)
        {
            return await PostRequest<bool>(null, baseLocalUrl + "api/oaiCtrlPanel/StopAllHarvestingJobs");
        }

        internal static async Task<bool> DeleteMetadata(string baseLocalUrl, List<DataProviderProperties> repositoryList)
        {
            return await PostRequest<bool>(repositoryList, baseLocalUrl + "api/oaiCtrlPanel/DeleteMetadata");
        }
    }
}