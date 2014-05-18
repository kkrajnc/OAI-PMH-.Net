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
    public static class Provenance
    {
        public static Metadata Decode(XElement metadata)
        {
            XElement root = metadata.Element("provenance").Element("originDescription");
            Metadata mainElement = DecodeOne(root);

            if (mainElement != null)
            {
                /* get nested origin descriptions */
                root = root.Element("originDescription");
                while (root != null)
                {
                    if (mainElement.NestedElements == null)
                    {
                        mainElement.NestedElements = new List<Metadata>();
                    }

                    Metadata tempMeta = DecodeOne(root);
                    if (tempMeta != null)
                    {
                        mainElement.NestedElements.Add(tempMeta);
                    }
                    root = root.Element("originDescription");
                }
            }

            return mainElement;
        }

        private static Metadata DecodeOne(XElement root)
        {
            if (root == null)
            {
                return null;
            }

            return new Metadata()
            {
                /* attribute - harvestDate */
                AdditionalDateTime1 = MlDecode.SafeDateTime(root.Attribute("harvestDate")),
                /* attribute - altered */
                AdditionalBool1 = new Func<bool>(() =>
                {
                    bool outVal;
                    XAttribute xa = root.Attribute("altered");
                    return xa == null ? false : bool.TryParse(xa.Value, out outVal) ? outVal : false;
                })(),
                /* element - baseURL */
                Source = MlDecode.Element(root, "baseURL"),
                Identifier = MlDecode.Element(root, "identifier"),
                /* element - datestamp */
                Date = MlDecode.SafeDateTime(root.Element("datestamp")),
                /* element - metadataNamespace */
                Format = MlDecode.Element(root, "metadataNamespace"),
                /* is harvest date in datetime format? */
                AdditionalBool2 = new Func<bool>(() =>
                {
                    XAttribute xa = root.Attribute("harvestDate");
                    return xa == null ? false : Helper.IsDateTimeFormat(xa.Value);
                })(),
                /* is datestamp in datetime format? */
                AdditionalBool3 = new Func<bool>(() =>
                {
                    XElement xe = root.Element("datestamp");
                    return xe == null ? false : Helper.IsDateTimeFormat(xe.Value);
                })(),
                MdFormat = (byte)Enums.MetadataFormats.Provenance
            };
        }

        public static XElement Encode(Metadata provenance, string granularity)
        {
            XElement mainElement = EncodeOne(provenance, granularity);

            if (provenance.NestedElements != null && provenance.NestedElements.Count > 0)
            {
                /* nest origin descriptions */
                XElement tempElement = mainElement;
                foreach (var item in provenance.NestedElements)
                {
                    tempElement.Add(EncodeOne(item, granularity));
                    tempElement = tempElement.Element("originDescription");
                }
            }

            return new XElement(MlNamespaces.provNs + "provenance",
                        new XAttribute(XNamespace.Xmlns + "xsi", MlNamespaces.provXsi),
                        new XAttribute(MlNamespaces.provXsi + "schemaLocation", MlNamespaces.provSchemaLocation),
                        mainElement
                       );
        }

        private static XElement EncodeOne(Metadata provenance, string granularity)
        {
            return new XElement("originDescription",
                        new XAttribute("harvestDate",
                            DateInGranularity(provenance.AdditionalDateTime1, provenance.AdditionalBool2, granularity)),
                        new XAttribute("altered",
                            !provenance.AdditionalBool1.HasValue ? bool.FalseString.ToLower() :
                                provenance.AdditionalBool1.Value.ToString().ToLower()),
                        MlEncode.Element("baseUrl", provenance.Source),
                        MlEncode.Element("identifier", provenance.Identifier),
                        new XAttribute("datestamp",
                            DateInGranularity(provenance.Date, provenance.AdditionalBool3, granularity)),
                        MlEncode.Element("metadataNamespace", provenance.Format)
                       );
        }

        private static string DateInGranularity(DateTime? date, bool? isDateTime, string defaultGranularity)
        {
            DateTime dateTime = !date.HasValue ? DateTime.MinValue : date.Value;
            string granularity = !isDateTime.HasValue ? defaultGranularity
                : isDateTime.Value ? Enums.Granularity.DateTime : Enums.Granularity.Date;

            return dateTime.ToUniversalTime().ToString(granularity);
        }

        public static void ReorderAboutList(ref List<Metadata> aboutList)
        {
            List<Metadata> provs = FormatList.GetAllMetadatasWithFormat(aboutList, Enums.MetadataFormats.Provenance).ToList();

            if (provs.Count > 1)
            {
                aboutList.RemoveAll(p => provs.Any(pr => pr.MetadataId == p.MetadataId));

                Metadata mainProv = provs.FirstOrDefault(p => p.AdditionalInt1 == null);
                if (mainProv != null)
                {
                    mainProv.NestedElements = new List<Metadata>();

                    /* find nested origin descriptions */
                    int prevId = mainProv.MetadataId;
                    Metadata tempProv = provs.FirstOrDefault(p => p.AdditionalInt1 == prevId);
                    while (tempProv != null)
                    {
                        mainProv.NestedElements.Add(tempProv);

                        /* find next */
                        prevId = tempProv.MetadataId;
                        tempProv = provs.FirstOrDefault(p => p.AdditionalInt1 == prevId);
                    }
                    aboutList.Add(mainProv);
                }
            }
        }

        public static void AddToDatabase(OaiPmhContext context, int objId, byte objType, byte metaType, Metadata provenance)
        {
            context.Metadata.Add(provenance);
            context.SaveChanges();
            context.ObjectMetadata.Add(new ObjectMetadata()
            {
                ObjectId = objId,
                ObjectType = objType,
                MetadataType = metaType,
                MetadataId = provenance.MetadataId
            });

            if (provenance.NestedElements != null)
            {
                int prevId = provenance.MetadataId;
                foreach (var item in provenance.NestedElements)
                {
                    item.AdditionalInt1 = prevId;

                    context.Metadata.Add(item);
                    context.SaveChanges();
                    context.ObjectMetadata.Add(new ObjectMetadata()
                    {
                        ObjectId = objId,
                        ObjectType = objType,
                        MetadataType = metaType,
                        MetadataId = item.MetadataId
                    });

                    prevId = item.MetadataId;
                }
            }
        }

        public static Metadata NewMeta(
            DateTime harvestDate,
            bool isHarvestDateTime,
            bool isAltered,
            string baseURL,
            string identifier,
            DateTime datestamp,
            bool isDatestampDateTime,
            string metadataNamespace)
        {
            return new Metadata()
            {
                AdditionalDateTime1 = harvestDate,
                AdditionalBool2 = isHarvestDateTime,
                AdditionalBool1 = isAltered,
                Source = baseURL,
                Identifier = identifier,
                Date = datestamp,
                AdditionalBool3 = isDatestampDateTime,
                Format = metadataNamespace,
                MdFormat = (byte)Enums.MetadataFormats.Provenance
            };
        }
    }
}