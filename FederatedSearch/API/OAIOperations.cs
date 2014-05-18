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

using FederatedSearch.API.Common;
using FederatedSearch.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Xml.Linq;

namespace FederatedSearch.API
{
    public static class OAIOperations
    {
        #region OAIDataProvider

        public static IEnumerable<OAIDataProvider> GetDataProviders()
        {
            using (var context = new OaiPmhContext())
            {
                context.Configuration.ProxyCreationEnabled = false; 
                return context.OAIDataProvider.ToList();
            }
        }

        private static OAIDataProvider IdentifyDataProvider(string baseURL)
        {
            if (!string.IsNullOrEmpty(baseURL))
            {
                try
                {
                    string body;
                    using (HttpClient client = new HttpClient())
                    {
                        body = client.GetStringAsync(baseURL + "?verb=Identify").Result;
                    }
                    XDocument xd = XDocument.Parse(body);

                    XElement root = xd.Root.Element(MlNamespaces.oaiNs + "Identify");

                    return OAIDataProvider.Decode(root);
                }
                catch (Exception) { }
            }

            return null;
        }

        public static OAIDataProvider AddOrUpdateDataProvider(string baseURL, OAIDataProvider dataProvider)
        {
            using (var context = new OaiPmhContext())
            {
                OAIDataProvider dp = null;
                bool isUpdateMode = dataProvider != null && dataProvider.OAIDataProviderId != 0;
                if (isUpdateMode)
                {
                    /* get data provider to update */
                    context.Configuration.ProxyCreationEnabled = false; 
                    dp = context.OAIDataProvider.Where(d => d.OAIDataProviderId == dataProvider.OAIDataProviderId).FirstOrDefault();
                }

                else if (!string.IsNullOrEmpty(baseURL))
                {
                    /* get and parse XML document */
                    dp = IdentifyDataProvider(baseURL);
                }

                if (dp != null && isUpdateMode ? true : !context.OAIDataProvider.Where(d => d.BaseURL == dp.BaseURL).Any())
                {
                    if (dataProvider != null)
                    {
                        dp.Function = dataProvider.Function;
                        dp.FirstSource = dataProvider.FirstSource;
                        dp.SecondSource = dataProvider.SecondSource;
                    }
                    if (!isUpdateMode)
                    {
                        context.OAIDataProvider.Add(dp);
                    }

                    context.SaveChanges();
                    return dp;
                }
            }

            return null;
        }

        public static bool DeleteDataProvider(int id)
        {
            using (var context = new OaiPmhContext())
            {
                var dataProvider = context.OAIDataProvider.Where(d => d.OAIDataProviderId == id).FirstOrDefault();
                if (dataProvider != null)
                {
                    context.OAIDataProvider.Remove(dataProvider);
                    context.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        internal static OAIDataProvider ReIdentifyDataProvider(int id)
        {
            using (var context = new OaiPmhContext())
            {
                context.Configuration.ProxyCreationEnabled = false; 
                var dataProvider = context.OAIDataProvider.Where(d => d.OAIDataProviderId == id).FirstOrDefault();
                if (dataProvider != null)
                {
                    var dp = IdentifyDataProvider(dataProvider.BaseURL);
                    if (dp != null)
                    {
                        dataProvider.AdminEmail = dp.AdminEmail;
                        dataProvider.BaseURL = dp.BaseURL;
                        dataProvider.Compression = dp.Compression;
                        dataProvider.DeletedRecord = dp.DeletedRecord;
                        dataProvider.EarliestDatestamp = dp.EarliestDatestamp;
                        dataProvider.Granularity = dp.Granularity;
                        dataProvider.ProtocolVersion = dp.ProtocolVersion;
                        dataProvider.RepositoryName = dp.RepositoryName;

                        context.SaveChanges();
                        return dataProvider;
                    }
                }
            }
            return null;
        }

        #endregion

        #region OAISetting

        public static bool AddOrUpdateSetting(Property newSetting)
        {
            if (!string.IsNullOrEmpty(newSetting.Key))
            {
                using (var context = new OaiPmhContext())
                {
                    if (newSetting.Value == null)
                    {
                        newSetting.Value = "";
                    }

                    Property setting = context.Property.Where(s => s.Key == newSetting.Key).FirstOrDefault();
                    if (setting == null)
                    {
                        /* add */
                        context.Property.Add(newSetting);
                        context.SaveChanges();
                        return true;
                    }

                    /* update */
                    setting.Value = newSetting.Value;
                    setting.Section = newSetting.Section;
                    context.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        public static bool DeleteSetting(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                using (var context = new OaiPmhContext())
                {
                    Property setting = context.Property.Where(s => s.Key == name).FirstOrDefault();
                    if (setting != null)
                    {
                        context.Property.Remove(setting);
                        context.SaveChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region Metadata

        public static bool DeleteMetadata(List<DataProviderProperties> dataProviders)
        {
            if (dataProviders != null || dataProviders.Count > 0)
            {
                try
                {
                    using (var context = new OaiPmhContext())
                    {
                        foreach (var dataProvider in dataProviders)
                        {
                            if (dataProvider.HarvestDeleteFiles)
                            {
                                var filesToDelete = (from h in context.Header
                                                     where h.OAIDataProviderId == dataProvider.OAIDataProviderId
                                                     select h.FilePath).ToList();

                                foreach (var file in filesToDelete)
                                {
                                    if (!string.IsNullOrEmpty(file) && File.Exists(file))
                                    {
                                        try
                                        {
                                            File.Delete(file);
                                        }
                                        catch (Exception) { }
                                    }
                                }
                            }

                            using (var cmd = context.Database.Connection.CreateCommand())
                            {
                                cmd.CommandText = "DeleteMetadata";
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add(new SqlParameter("dataProviderId", dataProvider.OAIDataProviderId));
                                cmd.Parameters.Add(new SqlParameter("fullDelete", dataProvider.FullHarvestDelete));
                                var retVal = new SqlParameter() { Direction = ParameterDirection.ReturnValue };
                                cmd.Parameters.Add(retVal);

                                try
                                {
                                    context.Database.Connection.Open();
                                    cmd.ExecuteNonQuery();
                                }
                                finally
                                {
                                    context.Database.Connection.Close();
                                }
                                return retVal.Value == null ? false : (int)retVal.Value != 0;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        private static void DeleteMetadataAndLink(OaiPmhContext context, ObjectMetadata objectMetadata, Metadata metadata)
        {
            if (metadata != null)
            {
                context.Metadata.Remove(metadata);
            }
            if (objectMetadata != null)
            {
                context.ObjectMetadata.Remove(objectMetadata);
            }
        }

        #endregion /* Metadata */
    }
}