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
using FederatedSearch.API.Internal;
using FederatedSearch.API.MdFormats;
using FederatedSearch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace FederatedSearch.API
{
    public class DataProvider
    {
        public static XDocument CheckAttributes(
            string verb,
            string from = null,
            string until = null,
            string metadataPrefix = null,
            string set = null,
            string resumptionToken = null,
            string identifier = null)
        {
            bool isVerb = !String.IsNullOrEmpty(verb);
            bool isFrom = !String.IsNullOrEmpty(from);
            bool isUntil = !String.IsNullOrEmpty(until);
            bool isPrefixOk = !String.IsNullOrEmpty(metadataPrefix);
            bool isSet = !String.IsNullOrEmpty(set);
            bool isResumption = !String.IsNullOrEmpty(resumptionToken);
            bool isIdentifier = !String.IsNullOrEmpty(identifier);

            if (!isVerb)
            {
                XElement request = new XElement("request", Properties.baseURL);
                return CreateXml(new XElement[] { request, MlErrors.badVerbMissing });
            }

            List<XElement> errors = new List<XElement>();
            switch (verb)
            {
                case "GetRecord":
                    {
                        if (isFrom) errors.Add(MlErrors.badFromArgumentNotAllowed);
                        if (isUntil) errors.Add(MlErrors.badUntilArgumentNotAllowed);
                        if (isSet) errors.Add(MlErrors.badSetArgumentNotAllowed);
                        if (isResumption) errors.Add(MlErrors.badResumptionArgumentNotAllowed);

                        return GetRecord(identifier, metadataPrefix, errors, Properties.loadAbout);
                    }
                case "Identify":
                    {
                        if (isFrom) errors.Add(MlErrors.badFromArgumentNotAllowed);
                        if (isUntil) errors.Add(MlErrors.badUntilArgumentNotAllowed);
                        if (isPrefixOk) errors.Add(MlErrors.badMetadataArgumentNotAllowed);
                        if (isSet) errors.Add(MlErrors.badSetArgumentNotAllowed);
                        if (isResumption) errors.Add(MlErrors.badResumptionArgumentNotAllowed);
                        if (isIdentifier) errors.Add(MlErrors.badIdentifierArgumentNotAllowed);

                        return Identify(errors);
                    }
                case "ListIdentifiers":
                case "ListRecords":
                    {
                        if (isIdentifier) errors.Add(MlErrors.badIdentifierArgumentNotAllowed);

                        return ListIdentifiersOrRecords(verb, from, until, metadataPrefix,
                            set, resumptionToken, false, errors, null);
                    }
                case "ListMetadataFormats":
                    {
                        if (isFrom) errors.Add(MlErrors.badFromArgumentNotAllowed);
                        if (isUntil) errors.Add(MlErrors.badUntilArgumentNotAllowed);
                        if (isPrefixOk) errors.Add(MlErrors.badMetadataArgumentNotAllowed);
                        if (isSet) errors.Add(MlErrors.badSetArgumentNotAllowed);
                        if (isResumption) errors.Add(MlErrors.badResumptionArgumentNotAllowed);

                        return ListMetadataFormats(identifier, errors);
                    }
                case "ListSets":
                    {
                        if (isFrom) errors.Add(MlErrors.badFromArgumentNotAllowed);
                        if (isUntil) errors.Add(MlErrors.badUntilArgumentNotAllowed);
                        if (isPrefixOk) errors.Add(MlErrors.badMetadataArgumentNotAllowed);
                        if (isSet) errors.Add(MlErrors.badSetArgumentNotAllowed);
                        if (isIdentifier) errors.Add(MlErrors.badIdentifierArgumentNotAllowed);

                        return ListSets(resumptionToken, false, errors);
                    }
                default:
                    {
                        XElement request = new XElement("request", Properties.baseURL);
                        return CreateXml(new XElement[] { request, MlErrors.badVerb });
                    }
            }
        }

        public static XDocument Identify(List<XElement> errorList)
        {
            XElement request = new XElement("request",
                                            new XAttribute("verb", "Identify"),
                                            Properties.baseURL);

            if (errorList.Count == 0)
            {
                XElement identify = new XElement("Identify",
                    new XElement("repositoryName", Properties.repositoryName),
                    new XElement("baseURL", Properties.baseURL),
                    new XElement("protocolVersion", Properties.protocolVersion),
                    new XElement("adminEmail", Properties.adminEmails),
                    new XElement("earliestDatestamp", Properties.earliestDatestamp),
                    new XElement("deletedRecord", Properties.deletedRecord),
                    new XElement("granularity", Properties.granularity.Replace("'", "")),
                    new XElement("compression", Properties.compression),
                    Properties.description != null ? Properties.description : null);

                return CreateXml(new XElement[] { request, identify });
            }

            errorList.Insert(0, request);
            return CreateXml(errorList.ToArray());
        }

        public static XDocument GetRecord(string identifier, string metadataPrefix, List<XElement> errorList, bool? loadAbout)
        {
            List<XElement> errors = errorList;

            bool isIdentifier = !String.IsNullOrEmpty(identifier);
            if (!isIdentifier)
            {
                errors.Add(MlErrors.badIdentifierArgument);
            }

            bool isPrefixOk = !String.IsNullOrEmpty(metadataPrefix);
            if (!isPrefixOk)
            {
                errors.Add(MlErrors.badMetadataArgument);
            }
            else if (FormatList.Prefix2Int(metadataPrefix) == 0)
            {
                errors.Add(MlErrors.cannotDisseminateFormat);
                isPrefixOk = false;
            }

            bool isAbout = loadAbout.HasValue ? loadAbout.Value : Properties.loadAbout;

            RecordQueryResult record = null;
            if (isIdentifier && isPrefixOk)
            {
                using (var context = new OaiPmhContext())
                {
                    Header header = DbQueries.GetHeader(context, identifier).FirstOrDefault();
                    if (header == null)
                    {
                        errors.Add(MlErrors.idDoesNotExist);
                    }
                    else
                    {
                        var formatNum = FormatList.Prefix2Int(metadataPrefix);

                        /* execute query */
                        var recQuery = (from om in context.ObjectMetadata
                                        join md in context.Metadata on om.MetadataId equals md.MetadataId
                                        where om.ObjectId == header.HeaderId
                                        where om.ObjectType == Enums.ObjectType.OAIRecord
                                        where ((om.MetadataType == Enums.MetadataType.Metadata && (md.MdFormat & formatNum) != 0) ||
                                               (om.MetadataType == Enums.MetadataType.About))
                                        group md by om.MetadataType into grp
                                        select grp).ToList();

                        record = new RecordQueryResult()
                        {
                            Header = header,
                            Metadata = (Metadata)recQuery.Where(g => g.Key == Enums.MetadataType.Metadata).SelectMany(g => g).FirstOrDefault(),
                            About = isAbout ? recQuery.Where(g => g.Key == Enums.MetadataType.About).SelectMany(g => g).Cast<Metadata>().ToList()
                                              : null
                        };

                        if (record == null || record.Metadata == null)
                        {
                            errors.Add(MlErrors.cannotDisseminateRecordFormat);
                        }
                    }
                }
            }

            XElement request = new XElement("request",
                new XAttribute("verb", "GetRecord"),
                isIdentifier ? new XAttribute("identifier", identifier) : null,
                isPrefixOk ? new XAttribute("metadataPrefix", metadataPrefix) : null,
                Properties.baseURL);

            if (errors.Count > 0)
            {
                errors.Insert(0, request); /* add request on the first position, that it will be diplayed before errors */
                return CreateXml(errors.ToArray());
            }


            XElement theRecord = new XElement("GetRecord",
                new XElement("record",
                    MlEncode.HeaderItem(record.Header, Properties.granularity),
                    MlEncode.Metadata(record.Metadata, Properties.granularity)),
                    isAbout ? MlEncode.About(record.About, Properties.granularity) : null);

            return CreateXml(new XElement[] { request, theRecord });
        }

        #region ListIdentifiers / ListRecords

        public static XDocument ListIdentifiersOrRecords(
            string verb,
            string from,
            string until,
            string metadataPrefix,
            string set,
            string resumptionToken,
            bool isRoundtrip,
            List<XElement> errorList,
            bool? loadAbout)
        {
            List<XElement> errors = errorList;
            DateTime? fromDate = DateTime.MinValue;
            DateTime? untilDate = DateTime.MaxValue;
            /* VERB */
            bool isRecord = false;
            if (String.IsNullOrEmpty(verb) || !(verb == "ListIdentifiers" || verb == "ListRecords"))
            {
                errors.Add(MlErrors.badVerbArgument);
            }
            else
            {
                isRecord = verb == "ListRecords";
            }
            /* FROM */
            bool isFrom = !String.IsNullOrEmpty(from);
            fromDate = MlDecode.SafeDateTime(from);
            if (isFrom && fromDate == null)
            {
                errors.Add(MlErrors.badFromArgument);
            }
            /* UNTIL */
            bool isUntil = !String.IsNullOrEmpty(until);
            untilDate = MlDecode.SafeDateTime(until);
            if (isUntil && untilDate == null)
            {
                errors.Add(MlErrors.badUntilArgument);
            }
            if (isFrom && isUntil && fromDate > untilDate)
            {
                errors.Add(MlErrors.badFromAndUntilArgument);
            }
            /* METADATA PREFIX */
            bool isPrefixOk = !String.IsNullOrEmpty(metadataPrefix);
            /* SETS */
            bool isSet = !String.IsNullOrEmpty(set);
            if (isSet && !Properties.supportSets)
            {
                errors.Add(MlErrors.noSetHierarchy);
            }
            /* RESUMPTION TOKEN */
            bool isResumption = !String.IsNullOrEmpty(resumptionToken);
            if (isResumption && !isRoundtrip)
            {
                if (isFrom || isUntil || isPrefixOk || isSet)
                {
                    errors.Add(MlErrors.badResumptionArgumentOnly);
                }

                if (!(Properties.resumptionTokens.ContainsKey(resumptionToken) &&
                    Properties.resumptionTokens[resumptionToken].Verb == verb &&
                    Properties.resumptionTokens[resumptionToken].ExpirationDate >= DateTime.UtcNow))
                {
                    errors.Insert(0, MlErrors.badResumptionArgument);
                }

                if (errors.Count == 0)
                {
                    return ListIdentifiersOrRecords(
                        verb,
                        Properties.resumptionTokens[resumptionToken].From.HasValue ?
                        Properties.resumptionTokens[resumptionToken].From.Value.ToUniversalTime().ToString(Properties.granularity) : null,
                        Properties.resumptionTokens[resumptionToken].Until.HasValue ? 
                        Properties.resumptionTokens[resumptionToken].Until.Value.ToUniversalTime().ToString(Properties.granularity) : null,
                        Properties.resumptionTokens[resumptionToken].MetadataPrefix,
                        Properties.resumptionTokens[resumptionToken].Set,
                        resumptionToken,
                        true,
                        errors,
                        loadAbout);
                }
            }

            if (!isPrefixOk) /* Check if the only required attribute is included in the request */
            {
                errors.Add(MlErrors.badMetadataArgument);
            }
            else if (FormatList.Prefix2Int(metadataPrefix) == 0)
            {
                errors.Add(MlErrors.cannotDisseminateFormat);
            }

            bool isAbout = loadAbout.HasValue ? loadAbout.Value : Properties.loadAbout;

            XElement request = new XElement("request",
                new XAttribute("verb", verb),
                isFrom ? new XAttribute("from", from) : null,
                isUntil ? new XAttribute("until", until) : null,
                isPrefixOk ? new XAttribute("metadataPrefix", metadataPrefix) : null,
                isSet ? new XAttribute("set", set) : null,
                isResumption ? new XAttribute("resumptionToken", resumptionToken) : null,
                Properties.baseURL);

            if (errors.Count > 0)
            {
                errors.Insert(0, request); /* add request on the first position, that it will be diplayed before errors */
                return CreateXml(errors.ToArray());
            }

            var records = new List<RecordQueryResult>();
            using (var context = new OaiPmhContext())
            {
                List<string> sets = Helper.GetAllSets(set);
                var formatNum = FormatList.Prefix2Int(metadataPrefix);

                var recordsQuery = from rec in context.Header
                                   join om in context.ObjectMetadata on rec.HeaderId equals om.ObjectId
                                   join md in context.Metadata on om.MetadataId equals md.MetadataId
                                   where om.ObjectType == Enums.ObjectType.OAIRecord
                                   where om.MetadataType == Enums.MetadataType.Metadata
                                   where (!isFrom || rec.Datestamp.Value >= fromDate)
                                   where (!isUntil || rec.Datestamp.Value <= untilDate)
                                   where (md.MdFormat & formatNum) != 0
                                   orderby rec.Datestamp
                                   select rec;
                if (isSet)
                {
                    recordsQuery = from rq in recordsQuery
                                   from s in context.OAISet
                                   from l in sets
                                   where s.Spec == l
                                   where rq.OAI_Set == s.Spec
                                   select rq;
                }

                int recordsCount = recordsQuery.Count();

                if (recordsCount == 0)
                {
                    return CreateXml(new XElement[] { request, MlErrors.noRecordsMatch });
                }
                else if (isRoundtrip)
                {
                    Properties.resumptionTokens[resumptionToken].CompleteListSize = recordsCount;
                    recordsQuery = recordsQuery.Skip(
                        Properties.resumptionTokens[resumptionToken].Cursor.Value).Take(
                        isRecord ? Properties.maxRecordsInList : Properties.maxIdentifiersInList);
                }
                else if ((isRecord ? Properties.resumeListRecords : Properties.resumeListIdentifiers) &&
                    (isRecord ? recordsCount > Properties.maxRecordsInList
                    : recordsCount > Properties.maxIdentifiersInList))
                {
                    resumptionToken = Helper.CreateGuid();
                    isResumption = true;
                    Properties.resumptionTokens.Add(resumptionToken,
                        new ResumptionToken()
                        {
                            Verb = verb,
                            From = isFrom ? fromDate : null,
                            Until = isUntil ? untilDate : null,
                            MetadataPrefix = metadataPrefix,
                            Set = set,
                            ExpirationDate = DateTime.UtcNow.Add(Properties.expirationTimeSpan),
                            CompleteListSize = recordsCount,
                            Cursor = 0
                        });

                    recordsQuery = recordsQuery.Take(
                        isRecord ? Properties.maxRecordsInList : Properties.maxIdentifiersInList);
                }

                /* get data from database */
                var recGroup = (from rec in recordsQuery
                                join omd in context.ObjectMetadata on rec.HeaderId equals omd.ObjectId
                                join mdt in context.Metadata on omd.MetadataId equals mdt.MetadataId
                                group new { OmdMetaType = omd.MetadataType, OaiMetaData = mdt } by rec into grp
                                select grp).ToList();

                /* distribute data into logical units */
                records = (from grp in recGroup
                           select new RecordQueryResult()
                           {
                               Header = grp.Key,
                               Metadata = isRecord ?
                                              grp.Where(g => g.OmdMetaType == Enums.MetadataType.Metadata).Select(g => g.OaiMetaData).FirstOrDefault() :
                                              null,
                               About = isRecord ?
                                           grp.Where(g => g.OmdMetaType == Enums.MetadataType.About).Select(g => g.OaiMetaData).ToList() :
                                           null
                           }).ToList();
            }

            bool isCompleted = isResumption ? 
                Properties.resumptionTokens[resumptionToken].Cursor + records.Count >= 
                Properties.resumptionTokens[resumptionToken].CompleteListSize : 
                false;

            XElement list = new XElement(verb,
                isRecord ?
                    GetListRecords(records, isAbout) :
                    GetListIdentifiers(records),
                isResumption ? /* add resumption token or not */
                    MlEncode.ResumptionToken(Properties.resumptionTokens[resumptionToken], resumptionToken, isCompleted)
                    : null);

            if (isResumption)
            {
                if (isCompleted)
                {
                    Properties.resumptionTokens.Remove(resumptionToken);
                }
                else
                {
                    Properties.resumptionTokens[resumptionToken].Cursor =
                        Properties.resumptionTokens[resumptionToken].Cursor + records.Count;
                }
            }

            return CreateXml(new XElement[] { request, list });
        }

        private static XElement[] GetListIdentifiers(List<RecordQueryResult> records)
        {
            return (from rec in records
                    select MlEncode.HeaderItem(rec.Header, Properties.granularity)).ToArray();
        }

        private static XElement[] GetListRecords(List<RecordQueryResult> records, bool isAbout)
        {
            return (from rec in records
                    select new XElement("record",
                    MlEncode.HeaderItem(rec.Header, Properties.granularity),
                    MlEncode.Metadata(rec.Metadata, Properties.granularity),
                    isAbout ? MlEncode.About(rec.About, Properties.granularity) : null
                   )).ToArray();
        }

        #endregion

        public static XDocument ListMetadataFormats(string identifier, List<XElement> errorList)
        {
            List<XElement> errors = errorList;

            bool isIdentifier = !String.IsNullOrEmpty(identifier);

            XElement request = new XElement("request",
                new XAttribute("verb", "ListMetadataFormats"),
                isIdentifier ? new XAttribute("identifier", identifier) : null,
                Properties.baseURL);

            List<OAIMetadataFormat> metadataFormats = new List<OAIMetadataFormat>();
            using (var context = new OaiPmhContext())
            {
                context.Configuration.LazyLoadingEnabled = false;

                if (isIdentifier)
                {
                    Header header = DbQueries.GetHeader(context, identifier).FirstOrDefault();
                    if (header == null)
                    {
                        errors.Add(MlErrors.idDoesNotExist);
                    }
                    else
                    {
                        int? recMetaFormats = (from omd in context.ObjectMetadata
                                               join mtd in context.Metadata on omd.MetadataId equals mtd.MetadataId
                                               where omd.ObjectType == Enums.ObjectType.OAIRecord
                                               where omd.ObjectId == header.HeaderId
                                               where omd.MetadataType == Enums.MetadataType.Metadata
                                               select mtd.MdFormat).FirstOrDefault();

                        metadataFormats = recMetaFormats.HasValue ? FormatList.GetAllFormatsFromInt(recMetaFormats.Value).ToList() : null;

                        if (metadataFormats == null || metadataFormats.Count == 0)
                        {
                            errors.Add(MlErrors.noMetadataFormats);
                        }
                    }
                }
                else
                {
                    metadataFormats = FormatList.List;
                }
            }

            if (errors.Count > 0)
            {
                errors.Insert(0, request); /* add request on the first position, that it will be diplayed before errors */
                return CreateXml(errors.ToArray());
            }

            XElement listMetadataFormats = new XElement("ListMetadataFormats",
                from mf in metadataFormats
                where mf.IsForList
                select new XElement("metadataFormat",
                    new XElement("metadataPrefix", mf.Prefix),
                    new XElement("schema", mf.Schema),
                    new XElement("metadataNamespace", mf.Namespace)));

            return CreateXml(new XElement[] { request, listMetadataFormats });
        }

        public static XDocument ListSets(string resumptionToken, bool isRoundtrip, List<XElement> errorList)
        {
            List<XElement> errors = errorList;

            if (!Properties.supportSets)
            {
                errors.Add(MlErrors.noSetHierarchy);
            }

            bool isResumption = !String.IsNullOrEmpty(resumptionToken);
            if (isResumption && !isRoundtrip)
            {
                if (!(Properties.resumptionTokens.ContainsKey(resumptionToken) &&
                    Properties.resumptionTokens[resumptionToken].Verb == "ListSets" &&
                    Properties.resumptionTokens[resumptionToken].ExpirationDate >= DateTime.UtcNow))
                {
                    errors.Insert(0, MlErrors.badResumptionArgument);
                }

                if (errors.Count == 0)
                {
                    return ListSets(resumptionToken, true, new List<XElement>());
                }
            }

            XElement request = new XElement("request",
                new XAttribute("verb", "ListSets"),
                isResumption ? new XAttribute("resumptionToken", resumptionToken) : null,
                Properties.baseURL);

            if (errors.Count > 0)
            {
                errors.Insert(0, request); /* add request on the first position, that it will be diplayed before errors */
                return CreateXml(errors.ToArray());
            }

            var sets = new List<Set>();
            using (var context = new OaiPmhContext())
            {
                context.Configuration.LazyLoadingEnabled = false;

                int setsCount = context.OAISet.Count();

                var setsQuery = from s in context.OAISet
                                join omd in context.ObjectMetadata on s.SetId equals omd.ObjectId
                                join mdt in context.Metadata on omd.MetadataId equals mdt.MetadataId
                                where omd.ObjectType == Enums.ObjectType.OAISet
                                orderby s.Name
                                group mdt by s into grp
                                select grp;

                if (isRoundtrip)
                {
                    Properties.resumptionTokens[resumptionToken].CompleteListSize = setsCount;
                    setsQuery = setsQuery.Skip(
                        Properties.resumptionTokens[resumptionToken].Cursor.Value).Take(
                        Properties.maxSetsInList);
                }
                else if (Properties.resumeListSets && setsCount > Properties.maxSetsInList)
                {
                    resumptionToken = Helper.CreateGuid();
                    isResumption = true;
                    Properties.resumptionTokens.Add(resumptionToken,
                        new ResumptionToken()
                        {
                            Verb = "ListSets",
                            ExpirationDate = DateTime.UtcNow.Add(Properties.expirationTimeSpan),
                            CompleteListSize = setsCount,
                            Cursor = 0
                        });

                    setsQuery = setsQuery.Take(Properties.maxSetsInList);
                }

                /* execute query */
                sets = (from g in setsQuery
                        select g.Key.MergeSetAndDescription(g.ToList())).ToList();

            }

            bool isCompleted = isResumption ? 
                Properties.resumptionTokens[resumptionToken].Cursor + sets.Count == 
                Properties.resumptionTokens[resumptionToken].CompleteListSize : 
                false;

            XElement list = new XElement("ListSets",
                from s in sets
                select new XElement("set",
                    new XElement("setSpec", s.Spec),
                    new XElement("setName", s.Name),
                    String.IsNullOrEmpty(s.Description) ? null
                        : new XElement("setDescription", s.Description),
                    MlEncode.SetDescription(s.AdditionalDescriptions, Properties.granularity)),
                isResumption ? /* add resumption token or not */
                    MlEncode.ResumptionToken(Properties.resumptionTokens[resumptionToken], resumptionToken, isCompleted)
                    : null);

            if (isResumption)
            {
                if (isCompleted)
                {
                    Properties.resumptionTokens.Remove(resumptionToken);
                }
                else
                {
                    Properties.resumptionTokens[resumptionToken].Cursor =
                        Properties.resumptionTokens[resumptionToken].Cursor + sets.Count;
                }
            }

            return CreateXml(new XElement[] { request, list });
        }

        /// <summary>
        /// Creates response xml document
        /// </summary>
        /// <param name="oaiElements">First oai element should be request and second should be either an error 
        /// or element with the same name as the verb.</param>
        /// <returns>Complete response aml document</returns>
        private static XDocument CreateXml(XElement[] oaiElements)
        {
            foreach (XElement xe in oaiElements)
            {
                SetDefaultXNamespace(xe, MlNamespaces.oaiNs);
            }

            return new XDocument(new XDeclaration("1.0", "utf-8", "no"),
                new XElement(MlNamespaces.oaiNs + "OAI-PMH",
                    new XAttribute(XNamespace.Xmlns + "xsi", MlNamespaces.oaiXsi),
                    new XAttribute(MlNamespaces.oaiXsi + "schemaLocation", MlNamespaces.oaiSchemaLocation),
                        new XElement(MlNamespaces.oaiNs + "responseDate",
                            DateTime.Now.ToUniversalTime().ToString(Enums.Granularity.DateTime)),
                            oaiElements));
        }

        public static void SetDefaultXNamespace(XElement xelem, XNamespace xmlns)
        {
            if (xelem.Name.NamespaceName == string.Empty)
                xelem.Name = xmlns + xelem.Name.LocalName;

            foreach (var xe in xelem.Elements())
                SetDefaultXNamespace(xe, xmlns);
        }
    }
}