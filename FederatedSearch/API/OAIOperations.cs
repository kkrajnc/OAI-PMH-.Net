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

        public static IEnumerable<OAIDataProvider> GetRepositoryList()
        {
            using (var context = new OaiPmhContext())
            {
                return context.OAIDataProvider.ToList();
            }
        }

        public static OAIDataProvider AddRepository(string baseURL)
        {
            if (!string.IsNullOrEmpty(baseURL))
            {
                /* get and parse XML document */
                string body;
                using (HttpClient client = new HttpClient())
                {
                    body = client.GetStringAsync(baseURL + "?verb=Identify").Result;
                }
                XDocument xd = XDocument.Parse(body);

                XElement root = xd.Root.Element(MlNamespaces.oaiNs + "Identify");

                OAIDataProvider dp = OAIDataProvider.Decode(root);
                if (dp != null)
                {
                    using (var context = new OaiPmhContext())
                    {
                        if (!context.OAIDataProvider.Where(d => d.BaseURL == dp.BaseURL).Any())
                        {
                            context.OAIDataProvider.Add(dp);
                            context.SaveChanges();
                            return dp;
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        #region OAISetting

        public static bool AddOrUpdateSetting(OAISetting newSetting)
        {
            if (!string.IsNullOrEmpty(newSetting.Key))
            {
                using (var context = new OaiPmhContext())
                {
                    if (newSetting.Value == null)
                    {
                        newSetting.Value = "";
                    }

                    OAISetting setting = context.OAISetting.Where(s => s.Key == newSetting.Key).FirstOrDefault();
                    if (setting == null)
                    {
                        /* add */
                        context.OAISetting.Add(newSetting);
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
                    OAISetting setting = context.OAISetting.Where(s => s.Key == name).FirstOrDefault();
                    if (setting != null)
                    {
                        context.OAISetting.Remove(setting);
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