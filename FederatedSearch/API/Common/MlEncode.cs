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

using FederatedSearch.API.MdFormats;
using FederatedSearch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace FederatedSearch.API.Common
{
    public class MlEncode
    {
        public static XElement HeaderItem(Header rec, string granularity)
        {
            return rec == null ? null
                : new XElement("header",
                    new XElement("identifier", rec.OAI_Identifier),
                    new XElement("datestamp", rec.Datestamp.HasValue ? rec.Datestamp.Value.ToString(granularity) : rec.Datestamp.ToString()),
                    from s in rec.OAI_Set.Split(';')
                    select new XElement("setSpec", s));
        }
        public static async Task<XElement> HeaderItemAsync(Header rec, string granularity)
        {
            return await Task.Run<XElement>(() => HeaderItem(rec, granularity)).ConfigureAwait(false);
        }


        public static XElement Metadata(Metadata metadata, string granularity)
        {
            return Metadata("metadata", metadata, granularity);
        }
        public static XElement Metadata(string containerName, Metadata metadata, string granularity)
        {
            if (string.IsNullOrEmpty(containerName) || metadata == null || string.IsNullOrEmpty(granularity))
            {
                return null;
            }

            switch (FormatList.Int2Format(metadata.MdFormat))
            {
                case Enums.MetadataFormats.DublinCore:
                    return new XElement(containerName, DublinCore.Encode(metadata, granularity));
                case Enums.MetadataFormats.Provenance:
                    return new XElement(containerName, Provenance.Encode(metadata, granularity));

                // TODO: Add format here

                case Enums.MetadataFormats.None:
                default:
                    return null;
            }
        }
        public static async Task<XElement> MetadataAsync(string containerName, Metadata metadata, string granularity)
        {
            return await Task.Run<XElement>(() => Metadata(containerName, metadata, granularity)).ConfigureAwait(false);
        }

        public static IEnumerable<XElement> MetaList(string containerName, IList<Metadata> metaList, string granularity)
        {
            if (string.IsNullOrEmpty(containerName) || metaList == null || metaList.Count == 0 || string.IsNullOrEmpty(granularity))
            {
                yield break;
            }

            foreach (var metaItem in metaList)
            {
                switch (FormatList.Int2Format(metaItem.MdFormat))
                {
                    case Enums.MetadataFormats.DublinCore:
                        yield return new XElement(containerName, DublinCore.Encode(metaItem, granularity));
                        break;
                    case Enums.MetadataFormats.Provenance:
                        yield return new XElement(containerName, Provenance.Encode(metaItem, granularity));
                        break;

                    // TODO: Add format here

                    case Enums.MetadataFormats.None:
                    default:
                        break;
                }
            }
        }

        public static IEnumerable<XElement> About(List<Metadata> aboutList, string granularity)
        {
            Provenance.ReorderAboutList(ref aboutList);

            return MetaList("about", aboutList, granularity);
        }
        public static async Task<IEnumerable<XElement>> AboutAsync(List<Metadata> aboutList, string granularity)
        {
            return await Task.Run<IEnumerable<XElement>>(() => About(aboutList, granularity)).ConfigureAwait(false);
        }

        public static IEnumerable<XElement> SetDescription(IList<Metadata> setDescList, string granularity)
        {
            return MetaList("setDescription", setDescList, granularity);
        }
        public static async Task<IEnumerable<XElement>> SetDescriptionAsync(IList<Metadata> setDescList, string granularity)
        {
            return await Task.Run<IEnumerable<XElement>>(() => SetDescription(setDescList, granularity)).ConfigureAwait(false);
        }

        public static IEnumerable<XElement> Element(XName element, string value)
        {
            return string.IsNullOrEmpty(value) ? new List<XElement>()
                    : from val in value.Split(new string[] { "][" }, StringSplitOptions.RemoveEmptyEntries)
                      select val.IndexOf("}{") != 0 ?
                      new XElement(element, val)
                      : SplitAttributes(element, val);
        }

        private static XElement SplitAttributes(XName element, string value)
        {
            XElement el = new XElement(element);
            string[] attributes = value.Split(new string[] { "}{" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < attributes.Length - 1; i++)
            {
                int seperatorIndex = attributes[i].IndexOf('=');
                if (seperatorIndex > 0)
                {
                    el.Add(new XAttribute(attributes[i].Substring(0, seperatorIndex)
                                            .Replace("{xmlns}", "{http://www.w3.org/XML/1998/namespace}"),
                                            attributes[i].Substring(seperatorIndex + 1)));
                }
            }

            /* last item in attributes should be elements value */
            el.Value = attributes.Last();

            return el;
        }

        public static XElement ResumptionToken(ResumptionToken rt, string resumptionToken, bool isCompleted)
        {
            return new XElement("resumptionToken",
                !rt.ExpirationDate.HasValue ? null :
                new XAttribute("expirationDate",
                    rt.ExpirationDate.Value.ToUniversalTime().ToString(Enums.Granularity.DateTime)),

                    rt.CompleteListSize.HasValue ?
                    new XAttribute("completeListSize", rt.CompleteListSize.Value) : null,

                    rt.Cursor.HasValue ?
                    new XAttribute("cursor", rt.Cursor.Value) : null,

                    isCompleted ? null : resumptionToken);
        }

        public static XElement Error(string code, string content)
        {
            return new XElement("error",
                new XAttribute("code", code),
                content);
        }

        public static IEnumerable<string> LimitElemenetsOnLang(string value, string lang, bool addAllWithoutAttribute)
        {
            try
            {
                return from e in Element("el", value)
                       where (e.HasAttributes && e.Attribute(XNamespace.Xml + "lang").Value == lang)
                                || (addAllWithoutAttribute && !e.HasAttributes)
                       select e.Value;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public static IEnumerable<string> ListElementValues(string value)
        {
            return from e in Element("el", value)
                   select e.Value;
        }
    }
}