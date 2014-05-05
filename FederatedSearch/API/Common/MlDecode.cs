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
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using FederatedSearch.API;
using FederatedSearch.API.MdFormats;
using System.Text;
using FederatedSearch.Models;
using System.Data.SqlTypes;

namespace FederatedSearch.API.Common
{
    public class MlDecode
    {
        public static void ResponseDate(ref XDocument xd, out DateTime responseDate, out bool isResponseDateTime)
        {
            XElement resDate = xd.Root.Element(MlNamespaces.oaiNs + "responseDate");
            if (resDate != null && Helper.ConvertUTCToDateTime(resDate.Value, out responseDate))
            {
                isResponseDateTime = resDate.Value.Length == 20;
            }
            else
            {
                responseDate = DateTime.UtcNow;
                isResponseDateTime = false;
            }
        }


        public static DateTime? SafeDateTime(XAttribute xa)
        {
            return SafeDateTime(xa.Value);
        }
        public static DateTime? SafeDateTime(XElement xe)
        {
            return SafeDateTime(xe.Value);
        }
        public static DateTime? SafeDateTime(string value)
        {
            DateTime date;
            if (!string.IsNullOrEmpty(value) && Helper.ConvertUTCToDateTime(value, out date))
            {
                if (date < SqlDateTime.MinValue.Value)
                {
                    date = SqlDateTime.MinValue.Value;
                }
                return date as DateTime?;
            }
            return null;
        }

        public static Header Header(XElement header)
        {
            if (header == null)
            {
                return null;
            }

            return new Header()
            {
                OAI_Identifier = header.Element(MlNamespaces.oaiNs + "identifier").Value,
                Datestamp = SafeDateTime(header.Element(MlNamespaces.oaiNs + "datestamp")),
                IsDatestampDateTime = new Func<bool>(() =>
                {
                    XElement xe = header.Element(MlNamespaces.oaiNs + "datestamp");
                    return xe == null ? false : xe.Value.Length == 20;
                })(),
                OAI_Set = new Func<string>(() =>
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (XElement set in header.Elements(MlNamespaces.oaiNs + "setSpec"))
                    {
                        sb.Append(set.Value + ";");
                    }
                    return sb.ToString();
                })(),
                Deleted = false,
                EnterDate = DateTime.UtcNow
            };
        }
        public static async Task<Header> HeaderAsync(XElement header)
        {
            return await Task.Run<Header>(() => Header(header)).ConfigureAwait(false);
        }

        public static Metadata Metadata(XElement metadata, string format)
        {
            if (metadata == null || string.IsNullOrEmpty(format))
            {
                return null;
            }

            switch (format)
            {
                case "oai_dc":
                    return DublinCore.Decode(metadata);
                case "provenance":
                    return Provenance.Decode(metadata);

                // TODO: Add format here

                default: return null;
            }
        }
        public static async Task<Metadata> MetadataAsync(XElement metadata, string format)
        {
            return await Task.Run<Metadata>(() => Metadata(metadata, format)).ConfigureAwait(false);
        }

        public static IEnumerable<Metadata> MetaList(IList<XElement> metaList)
        {
            if (metaList == null || metaList.Count == 0)
            {
                yield break;
            }

            foreach (var item in metaList)
            {
                switch (item.Name.LocalName)
                {
                    case "oai_dc":
                        yield return DublinCore.Decode(item);
                        break;
                    case "provenance":
                        yield return Provenance.Decode(item);
                        break;

                    // TODO: Add format here

                    default:
                        break;
                }
            }
        }
        public static async Task<IEnumerable<Metadata>> MetaListAsync(IList<XElement> metaList)
        {
            return await Task.Run<IEnumerable<Metadata>>(() => MetaList(metaList)).ConfigureAwait(false);
        }

        public static string Element(XElement root, XName element)
        {
            StringBuilder result = new StringBuilder();
            foreach (XElement el in root.Elements(element))
            {
                if (el.HasAttributes)
                {
                    result.Append("}{");
                    foreach (var attribute in el.Attributes())
                    {
                        result.Append(attribute.Name + "=" + attribute.Value + "}{");
                    }
                }
                result.Append(el.Value + "][");
            }
            return result.Length > 2 ? result.ToString()
                                        .Substring(0, result.Length - 2)
                                        .Replace("{http://www.w3.org/XML/1998/namespace}", "{xmlns}") : null;
        }
    }
}