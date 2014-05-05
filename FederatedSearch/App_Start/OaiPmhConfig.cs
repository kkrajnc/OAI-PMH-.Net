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
            props["RepositoryName"] = new OAISetting() { Key = "RepositoryName", Value = "Test repository", Section = "ip" };
            props["BaseURL"] = new OAISetting() { Key = "BaseURL", Value = "http://localhost:1793/api/oai", Section = "ip" };
            props["ProtocolVersion"] = new OAISetting() { Key = "ProtocolVersion", Value = "2.0", Section = "ip" };
            props["EarliestDatestamp"] = new OAISetting() { Key = "EarliestDatestamp", Value = SqlDateTime.MinValue.Value.ToString(Enums.Granularity.DateTime), Section = "ip" };
            props["DeletedRecord"] = new OAISetting() { Key = "DeletedRecord", Value = Enums.DeletedRecords.No, Section = "ip" };
            props["Granularity"] = new OAISetting() { Key = "Granularity", Value = Enums.Granularity.DateTime, Section = "ip" };
            props["AdminEmails"] = new OAISetting() { Key = "AdminEmails", Value = "test@domain.com", Section = "ip" };
            props["Compression"] = new OAISetting() { Key = "Compression", Value = null, Section = "ip" };
            props["Description"] = new OAISetting() { Key = "Description", Value = null, Section = "ip" };
            /* Data provider properties */
            props["SupportSets"] = new OAISetting() { Key = "SupportSets", Value = "False", Section = "dpp" };
            props["ResumeListSets"] = new OAISetting() { Key = "ResumeListSets", Value = "False", Section = "dpp" };
            props["MaxSetsInList"] = new OAISetting() { Key = "MaxSetsInList", Value = "30", Section = "dpp" };
            props["ResumeListIdentifiers"] = new OAISetting() { Key = "ResumeListIdentifiers", Value = "True", Section = "dpp" };
            props["MaxIdentifiersInList"] = new OAISetting() { Key = "MaxIdentifiersInList", Value = "100", Section = "dpp" };
            props["ResumeListRecords"] = new OAISetting() { Key = "ResumeListRecords", Value = "True", Section = "dpp" };
            props["MaxRecordsInList"] = new OAISetting() { Key = "MaxRecordsInList", Value = "30", Section = "dpp" };
            props["ExpirationTimeSpan"] = new OAISetting() { Key = "ExpirationTimeSpan", Value = new TimeSpan(1, 0, 0, 0).ToString(), Section = "dpp" };
            props["LoadAbout"] = new OAISetting() { Key = "LoadAbout", Value = "True", Section = "dpp" };
            /* Harvester properties */
            props["ValidateXml"] = new OAISetting() { Key = "ValidateXml", Value = "False", Section = "hp" };
            props["HarvestAbout"] = new OAISetting() { Key = "HarvestAbout", Value = "True", Section = "hp" };
            props["AddProvenanceToHarvestedRecords"] = new OAISetting() { Key = "AddProvenanceToHarvestedRecords", Value = "True", Section = "hp" };
            props["CreateNewIdentifierForHarvestedRecords"] = new OAISetting() { Key = "CreateNewIdentifierForHarvestedRecords", Value = "False", Section = "hp" };
            props["IdentifierBase"] = new OAISetting() { Key = "IdentifierBase", Value = "oai:test.org:", Section = "hp" };
            props["MinTimeBetweenRequests"] = new OAISetting() { Key = "MinTimeBetweenRequests", Value = new TimeSpan(0, 0, 30).ToString(), Section = "hp" };
            props["RetryRetrievalCount"] = new OAISetting() { Key = "RetryRetrievalCount", Value = "3", Section = "hp" };
            props["OverwriteHarvestedFiles"] = new OAISetting() { Key = "OverwriteHarvestedFiles", Value = "False", Section = "hp" };
            props["LimitHarvestedFileTypes"] = new OAISetting() { Key = "LimitHarvestedFileTypes", Value = "True", Section = "hp" };
            props["AllowedMimeTypes"] = new OAISetting()
            {
                Key = "AllowedMimeTypes", 
                Value = "application/pdf;" + 
                        "application/msword;" +
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
                Section = "hp" };
            props["PropertySections"] = new OAISetting()
            { 
                Key = "PropertySections", 
                Value = "ip=Identify;" +
                        "dpp=Dataprovider;" +
                        "hp=Harvester;" +
                        "pfhp=Page fileharvester;" +
                        "tp=Test properties",
                Section = "hp"
            };
            /* Page file harvester properties */
            props["http://dkum.uni-mb.si"] = new OAISetting()
            {
                Key = "http://dkum.uni-mb.si",
                Value = JsonConvert.SerializeObject(new PageFileHarvestProperties()
                    {
                        BaseUri = "http://dkum.uni-mb.si",
                        FirstHttpMethod = "GET",
                        SecondHttpMethod = "POST",
                        LineRegex = @"<input type=""hidden"" name=""key"" value=""[a-zA-Z0-9]+"" />",
                        ValueRegex = @"(?<=\bvalue="")[^""]*",
                        SecondTierValueOption = "Key"
                    }),
                Section = "pfhp"
            };

            /* Other ... */
            props.SetSchemaSet(MlNamespaces.oaiNs.ToString(), Directory.GetCurrentDirectory() + "\\oaiSchema.xsd");

            Properties.UpdateFromDatabase();

            Properties.UpdateMimeTypeList();
            Properties.UpdatePropertySections();
            Properties.UpdatePageFileHarvestProperties();
        }
    }
}