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
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Web.Routing;
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using Newtonsoft.Json;

namespace FederatedSearch.API /* .Common */
{
    public class Properties
    {
        #region Required properties

        private static ConcurrentDictionary<string, OAISetting> properties = new ConcurrentDictionary<string, OAISetting>();
        public static XmlSchemaSet schemas = new XmlSchemaSet();
        public static Dictionary<string, ResumptionToken> resumptionTokens = new Dictionary<string, ResumptionToken>();
        public static List<string> allowedMimeTypesList = new List<string>();
        public static ConcurrentDictionary<string, string> propertySectionsTable = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, PageFileHarvestProperties> pageFileHarvestProperties = new ConcurrentDictionary<string, PageFileHarvestProperties>();

        #endregion

        #region /* Identify properties -------------------------------------------------------------*/

        public static string repositoryName
        {
            get { return GetStringProperty("RepositoryName"); }
            set { SetProperty("RepositoryName", value); }
        }
        public static string baseURL
        {
            get { return GetStringProperty("BaseURL"); }
            set { SetProperty("BaseURL", value); }
        }
        public static string protocolVersion
        {
            get { return GetStringProperty("ProtocolVersion"); }
            set { SetProperty("ProtocolVersion", value); }
        }
        public static string earliestDatestamp
        {
            get { return GetStringProperty("EarliestDatestamp"); }
            set { SetProperty("EarliestDatestamp", value); }
        }
        public static string deletedRecord
        {
            get { return GetStringProperty("DeletedRecord"); }
            set { SetProperty("DeletedRecord", value); }
        }
        public static string granularity
        {
            get { return GetStringProperty("Granularity"); }
            set { SetProperty("Granularity", value); }
        }
        public static string adminEmails
        {
            get { return GetStringProperty("AdminEmails"); }
            set { SetProperty("AdminEmails", value); }
        }
        public static string compression
        {
            get { return GetStringProperty("Compression"); }
            set { SetProperty("Compression", value); }
        }
        public static string description
        {
            get { return GetStringProperty("Description"); }
            set { SetProperty("Description", value); }
        }

        #endregion

        #region /* Other dp properties -------------------------------------------------------------*/

        public static bool supportSets
        {
            get { return GetBoolProperty("SupportSets"); }
            set { SetProperty("SupportSets", value); }
        }
        public static bool resumeListSets
        {
            get { return GetBoolProperty("ResumeListSets"); }
            set { SetProperty("ResumeListSets", value); }
        }
        public static int maxSetsInList
        {
            get { return GetIntProperty("MaxSetsInList"); }
            set { SetProperty("MaxSetsInList", value); }
        }
        public static bool resumeListIdentifiers
        {
            get { return GetBoolProperty("ResumeListIdentifiers"); }
            set { SetProperty("ResumeListIdentifiers", value); }
        }
        public static int maxIdentifiersInList
        {
            get { return GetIntProperty("MaxIdentifiersInList"); }
            set { SetProperty("MaxIdentifiersInList", value); }
        }
        public static bool resumeListRecords
        {
            get { return GetBoolProperty("ResumeListRecords"); }
            set { SetProperty("ResumeListRecords", value); }
        }
        public static int maxRecordsInList
        {
            get { return GetIntProperty("MaxRecordsInList"); }
            set { SetProperty("MaxRecordsInList", value); }
        }
        public static TimeSpan expirationTimeSpan
        {
            get { return GetTimeSpanProperty("ExpirationTimeSpan"); }
            set { SetProperty("ExpirationTimeSpan", value); }
        }
        public static bool loadAbout
        {
            get { return GetBoolProperty("LoadAbout"); }
            set { SetProperty("LoadAbout", value); }
        }

        #endregion

        #region /* Harvester properties ------------------------------------------------------------*/

        public static bool validateXml
        {
            get { return GetBoolProperty("ValidateXml"); }
            set { SetProperty("ValidateXml", value); }
        }
        public static bool harvestAbout
        {
            get { return GetBoolProperty("HarvestAbout"); }
            set { SetProperty("HarvestAbout", value); }
        }
        public static bool addProvenanceToHarvestedRecords
        {
            get { return GetBoolProperty("AddProvenanceToHarvestedRecords"); }
            set { SetProperty("AddProvenanceToHarvestedRecords", value); }
        }
        public static bool createNewIdentifierForHarvestedRecords
        {
            get { return GetBoolProperty("CreateNewIdentifierForHarvestedRecords"); }
            set { SetProperty("CreateNewIdentifierForHarvestedRecords", value); }
        }
        public static string identifierBase
        {
            get { return GetStringProperty("IdentifierBase"); }
            set { SetProperty("IdentifierBase", value); }
        }
        public static TimeSpan minTimeBetweenRequests
        {
            get { return GetTimeSpanProperty("MinTimeBetweenRequests"); }
            set { SetProperty("MinTimeBetweenRequests", value); }
        }
        public static int retryRetrievalCount
        {
            get { return GetIntProperty("RetryRetrievalCount"); }
            set { SetProperty("RetryRetrievalCount", value); }
        }
        public static bool overwriteHarvestedFiles
        {
            get { return GetBoolProperty("OverwriteHarvestedFiles"); }
            set { SetProperty("OverwriteHarvestedFiles", value); }
        }
        public static bool limitHarvestedFileTypes
        {
            get { return GetBoolProperty("LimitHarvestedFileTypes"); }
            set { SetProperty("LimitHarvestedFileTypes", value); }
        }
        public static string allowedMimeTypes
        {
            get { return GetStringProperty("AllowedMimeTypes"); }
            set
            {
                SetProperty("AllowedMimeTypes", value);
                UpdateMimeTypeList();
            }
        }
        public static string propertySections
        {
            get { return GetStringProperty("PropertySections"); }
            set
            {
                SetProperty("PropertySections", value);
                UpdatePropertySections();
            }
        }

        #endregion

        #region Getters and setter for different types

        /* string ----------------------------------------------------------------------------------*/
        private static string GetStringProperty(string name)
        {
            return GetStringProperty(name, null);
        }
        private static string GetStringProperty(string name, string defaultReturn)
        {
            OAISetting setting = null;
            if (properties.TryGetValue(name, out setting))
            {
                return setting.Value;
            }
            return defaultReturn;
        }

        /* bool ----------------------------------------------------------------------------------*/
        private static bool GetBoolProperty(string name)
        {
            return GetBoolProperty(name, false);
        }
        private static bool GetBoolProperty(string name, bool defaultReturn)
        {
            bool bValue = false;
            OAISetting setting = null;
            if (properties.TryGetValue(name, out setting) &&
                !string.IsNullOrEmpty(setting.Value) &&
                bool.TryParse(setting.Value, out bValue))
            {
                return bValue;
            }
            return defaultReturn;
        }

        /* int ----------------------------------------------------------------------------------*/
        private static int GetIntProperty(string name)
        {
            return GetIntProperty(name, 0);
        }
        private static int GetIntProperty(string name, int defaultReturn)
        {
            int iValue = 0;
            OAISetting setting = null;
            if (properties.TryGetValue(name, out setting) &&
                !string.IsNullOrEmpty(setting.Value) &&
                int.TryParse(setting.Value, out iValue))
            {
                return iValue;
            }
            return defaultReturn;
        }

        /* TimeSpan --------------------------------------------------------------------------------*/
        private static TimeSpan GetTimeSpanProperty(string name)
        {
            return GetTimeSpanProperty(name, TimeSpan.MinValue);
        }
        private static TimeSpan GetTimeSpanProperty(string name, TimeSpan defaultReturn)
        {
            TimeSpan tsValue = TimeSpan.MinValue;
            OAISetting setting = null;
            if (properties.TryGetValue(name, out setting) &&
                !string.IsNullOrEmpty(setting.Value) &&
                TimeSpan.TryParse(setting.Value, out tsValue))
            {
                return tsValue;
            }
            return defaultReturn;
        }

        public static void SetProperty(string name, object value)
        {
            OAISetting setting = null;
            if (properties.TryGetValue(name, out setting))
            {
                setting.Value = value.ToString();
                properties.AddOrUpdate(name, setting, (key, oldValue) => setting);
            }
        }

        #endregion

        /* -----------------------------------------------------------------------------------------*/
        public void SetSchemaSet(string targetNamespace, string schemaUri)
        {
            schemas.Add(targetNamespace, schemaUri);
        }

        public static IEnumerable<OAISetting> GetProperties()
        {
            return properties
                .OrderBy(p => p.Value.Section)
                .OrderBy(k => k.Key)
                .Select(p => p.Value);
        }

        public static IEnumerable<OAISetting> GetSectionProperties(string section)
        {
            if (string.IsNullOrEmpty(section))
            {
                return null;
            }

            return properties.Where(p => p.Value.Section == section)
                .OrderBy(k => k.Key)
                .Select(p => p.Value);
        }

        public static void UpdateFromDatabase()
        {
            var settings = new List<OAISetting>();
            using (var context = new OaiPmhContext())
            {
                settings = context.OAISetting.ToList();
            }
            foreach (var setting in settings)
            {
                properties.AddOrUpdate(setting.Key, setting, (key, oldValue) => setting);
            }
        }

        public static bool CheckForExistance(string name)
        {
            return properties.ContainsKey(name);
        }

        public static void AddOrUpdate(OAISetting setting)
        {
            properties.AddOrUpdate(setting.Key, setting, (key, oldValue) => setting);
        }

        public static bool Delete(string name)
        {
            OAISetting setting;
            return properties.TryRemove(name, out setting);
        }

        public static Dictionary<string, string> GetPropertySections()
        {
            return propertySectionsTable.ToDictionary(k => k.Key, v => v.Value);
        }

        public static List<PageFileHarvestProperties> GetPageFileHarvestProperties()
        {
            return pageFileHarvestProperties.Select(v => v.Value).ToList();
        }

        public static void UpdateMimeTypeList()
        {
            allowedMimeTypesList = allowedMimeTypes.Split(';').ToList();
        }

        public static void UpdatePropertySections()
        {
            propertySectionsTable = new ConcurrentDictionary<string, string>(
                propertySections.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ps => ps.Split('='))
                .Select(kv => new KeyValuePair<string, string>(kv[0].Trim(), kv[1].Trim())));
        }

        public static void UpdatePageFileHarvestProperties()
        {
            pageFileHarvestProperties = new ConcurrentDictionary<string, PageFileHarvestProperties>(
                properties.Where(p => p.Value.Section == "pfhp")
                .OrderBy(k => k.Key)
                .Select(p => new KeyValuePair<string, PageFileHarvestProperties>(p.Key,
                    Helper.TryToDeserializeJson<PageFileHarvestProperties>(p.Value.Value))));
        }

        public OAISetting this[string key]
        {
            get
            {
                return properties[key];
            }
            set
            {
                properties[key] = value;
            }
        }
    }
}