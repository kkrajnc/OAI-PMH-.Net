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
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace FederatedSearch
{
    public static class OaiPmhConfig
    {
        public static void Register()
        {
            using(var context = new OaiPmhContext())
            {
                context.Database.Initialize(true);
            }

            Properties props = new Properties();

            /* Set default settings */
            /* Identify properties */
            props["RepositoryName"] = new Property() { Key = "RepositoryName", Value = "Test repository", Section = "ip" };
            props["BaseURL"] = new Property() { Key = "BaseURL", Value = "http://localhost:1777/api/oai", Section = "ip" };
            props["ProtocolVersion"] = new Property() { Key = "ProtocolVersion", Value = "2.0", Section = "ip" };
            props["EarliestDatestamp"] = new Property() { Key = "EarliestDatestamp", Value = SqlDateTime.MinValue.Value.ToString(Enums.Granularity.DateTime), Section = "ip" };
            props["DeletedRecord"] = new Property() { Key = "DeletedRecord", Value = Enums.DeletedRecords.No, Section = "ip" };
            props["Granularity"] = new Property() { Key = "Granularity", Value = Enums.Granularity.DateTime, Section = "ip" };
            props["AdminEmails"] = new Property() { Key = "AdminEmails", Value = "test@domain.com", Section = "ip" };
            props["Compression"] = new Property() { Key = "Compression", Value = null, Section = "ip" };
            props["Description"] = new Property() { Key = "Description", Value = null, Section = "ip" };
            /* Data provider properties */
            props["SupportSets"] = new Property() { Key = "SupportSets", Value = "False", Section = "dpp" };
            props["ResumeListSets"] = new Property() { Key = "ResumeListSets", Value = "False", Section = "dpp" };
            props["MaxSetsInList"] = new Property() { Key = "MaxSetsInList", Value = "30", Section = "dpp" };
            props["ResumeListIdentifiers"] = new Property() { Key = "ResumeListIdentifiers", Value = "True", Section = "dpp" };
            props["MaxIdentifiersInList"] = new Property() { Key = "MaxIdentifiersInList", Value = "100", Section = "dpp" };
            props["ResumeListRecords"] = new Property() { Key = "ResumeListRecords", Value = "True", Section = "dpp" };
            props["MaxRecordsInList"] = new Property() { Key = "MaxRecordsInList", Value = "30", Section = "dpp" };
            props["ExpirationTimeSpan"] = new Property() { Key = "ExpirationTimeSpan", Value = new TimeSpan(1, 0, 0, 0).ToString(), Section = "dpp" };
            props["LoadAbout"] = new Property() { Key = "LoadAbout", Value = "True", Section = "dpp" };
            /* Harvester properties */
            props["ValidateXml"] = new Property() { Key = "ValidateXml", Value = "False", Section = "hp" };
            props["HarvestAbout"] = new Property() { Key = "HarvestAbout", Value = "True", Section = "hp" };
            props["AddProvenanceToHarvestedRecords"] = new Property() { Key = "AddProvenanceToHarvestedRecords", Value = "True", Section = "hp" };
            props["CreateNewIdentifierForHarvestedRecords"] = new Property() { Key = "CreateNewIdentifierForHarvestedRecords", Value = "False", Section = "hp" };
            props["IdentifierBase"] = new Property() { Key = "IdentifierBase", Value = "oai:test.org:", Section = "hp" };
            props["MinTimeBetweenRequests"] = new Property() { Key = "MinTimeBetweenRequests", Value = new TimeSpan(0, 0, 30).ToString(), Section = "hp" };
            props["RetryRetrievalCount"] = new Property() { Key = "RetryRetrievalCount", Value = "3", Section = "hp" };
            props["OverwriteHarvestedFiles"] = new Property() { Key = "OverwriteHarvestedFiles", Value = "False", Section = "hp" };
            props["LimitHarvestedFileTypes"] = new Property() { Key = "LimitHarvestedFileTypes", Value = "True", Section = "hp" };
            props["DirectoryForHarvestedFiles"] = new Property() { Key = "DirectoryForHarvestedFiles", Value = "C:\\OAIHarvestedFiles", Section = "hp" };
            props["AllowedMimeTypes"] = new Property()
            {
                Key = "AllowedMimeTypes", 
                Value = "application/pdf;" + 
                        "application/msword;" +
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
                Section = "hp" };
            props["PropertySections"] = new Property()
            { 
                Key = "PropertySections", 
                Value = "ip=Identify;" +
                        "dpp=Dataprovider;" +
                        "hp=Harvester;" +
                        "pfhp=Page fileharvester;",
                Section = "hp"
            };

            /* Other ... */
            if (File.Exists(Directory.GetCurrentDirectory() + "\\oaiSchema.xsd"))
            {
                props.SetSchemaSet(MlNamespaces.oaiNs.ToString(), Directory.GetCurrentDirectory() + "\\oaiSchema.xsd");
            }

            Properties.UpdateFromDatabase();

            Properties.UpdateMimeTypeList();
            Properties.UpdatePropertySections();
            Properties.UpdatePageFileHarvestProperties();
        }
    }
}