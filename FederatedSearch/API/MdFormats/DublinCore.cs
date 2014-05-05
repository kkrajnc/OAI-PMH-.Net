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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Threading.Tasks;
using FederatedSearch.API.Common;
using FederatedSearch.API.Internal;
using FederatedSearch.Models;

namespace FederatedSearch.API.MdFormats
{
    public static class DublinCore
    {
        public static Metadata Decode(XElement metadata)
        {
            XElement root = metadata.Element(MlNamespaces.oaiDc + "dc");

            return new Metadata()
            {
                Title = MlDecode.Element(root, MlNamespaces.dcNs + "title"),
                Creator = MlDecode.Element(root, MlNamespaces.dcNs + "creator"),
                Subject = MlDecode.Element(root, MlNamespaces.dcNs + "subject"),
                Description = MlDecode.Element(root, MlNamespaces.dcNs + "description"),
                Publisher = MlDecode.Element(root, MlNamespaces.dcNs + "publisher"),
                Contributor = MlDecode.Element(root, MlNamespaces.dcNs + "contributor"),
                Date = MlDecode.SafeDateTime(root.Element(MlNamespaces.dcNs + "date")),
                Type = MlDecode.Element(root, MlNamespaces.dcNs + "type"),
                Format = MlDecode.Element(root, MlNamespaces.dcNs + "format"),
                Identifier = MlDecode.Element(root, MlNamespaces.dcNs + "identifier"),
                Source = MlDecode.Element(root, MlNamespaces.dcNs + "source"),
                Language = MlDecode.Element(root, MlNamespaces.dcNs + "language"),
                Relation = MlDecode.Element(root, MlNamespaces.dcNs + "relation"),
                Coverage = MlDecode.Element(root, MlNamespaces.dcNs + "coverage"),
                Rights = MlDecode.Element(root, MlNamespaces.dcNs + "rights"),
                MdFormat = (byte)Enums.MetadataFormats.DublinCore
            };
        }

        public static XElement Encode(Metadata dublinCore, string granularity)
        {
            return new XElement(MlNamespaces.oaiDc + "dc",
                    new XAttribute(XNamespace.Xmlns + "oai_dc", MlNamespaces.oaiDc),
                    new XAttribute(XNamespace.Xmlns + "dc", MlNamespaces.dcNs),
                    new XAttribute(XNamespace.Xmlns + "xsi", MlNamespaces.dcXsi),
                    new XAttribute(MlNamespaces.dcXsi + "schemaLocation", MlNamespaces.dcSchemaLocation),
                    /* content */
                    MlEncode.Element(MlNamespaces.dcNs + "title", dublinCore.Title),
                    MlEncode.Element(MlNamespaces.dcNs + "creator", dublinCore.Creator),
                    MlEncode.Element(MlNamespaces.dcNs + "subject", dublinCore.Subject),
                    MlEncode.Element(MlNamespaces.dcNs + "description", dublinCore.Description),
                    MlEncode.Element(MlNamespaces.dcNs + "publisher", dublinCore.Publisher),
                    MlEncode.Element(MlNamespaces.dcNs + "contributor", dublinCore.Contributor),
                    !dublinCore.Date.HasValue ? null
                           : new XElement(MlNamespaces.dcNs + "date",
                               dublinCore.Date.Value.ToUniversalTime().ToString(granularity)),
                    MlEncode.Element(MlNamespaces.dcNs + "type", dublinCore.Type),
                    MlEncode.Element(MlNamespaces.dcNs + "format", dublinCore.Format),
                    MlEncode.Element(MlNamespaces.dcNs + "identifier", dublinCore.Identifier),
                    MlEncode.Element(MlNamespaces.dcNs + "source", dublinCore.Source),
                    MlEncode.Element(MlNamespaces.dcNs + "language", dublinCore.Language),
                    MlEncode.Element(MlNamespaces.dcNs + "relation", dublinCore.Relation),
                    MlEncode.Element(MlNamespaces.dcNs + "coverage", dublinCore.Coverage),
                    MlEncode.Element(MlNamespaces.dcNs + "rights", dublinCore.Rights)
                   );
        }

        public static void AddToDatabase(OaiPmhContext context, int objId, byte objType, byte metaType, Metadata dublinCore)
        {
            DbQueries.AddMetadataToDatabase(context, objId, objType, metaType, dublinCore);
        }
    }
}