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
using System.Web;

namespace FederatedSearch.API.MdFormats
{
    public static class FormatList
    {
        public static List<OAIMetadataFormat> List
        {
            get
            {
                var tmp = new List<OAIMetadataFormat>();
                tmp.Add(oai_dc);
                tmp.Add(provenance);

                // TODO: Add format here

                return tmp;
            }

            private set
            { }
        }

        public static OAIMetadataFormat oai_dc = new OAIMetadataFormat()
        {
            Prefix = "oai_dc",
            Schema = "http://www.openarchives.org/OAI/2.0/oai_dc.xsd",
            Namespace = "http://www.openarchives.org/OAI/2.0/oai_dc/",
            FormatNum = Enums.MetadataFormats.DublinCore,
            IsForList = true
        };

        public static OAIMetadataFormat provenance = new OAIMetadataFormat()
        {
            Prefix = "provenance",
            Schema = "http://www.openarchives.org/OAI/2.0/provenance.xsd",
            Namespace = "http://www.openarchives.org/OAI/2.0/provenance",
            FormatNum = Enums.MetadataFormats.Provenance,
            IsForList = false
        };

        // TODO: Add format here

        public static int Prefix2Int(string metadataPrefix)
        {
            switch (metadataPrefix)
            {
                case "oai_dc":
                    return (int)Enums.MetadataFormats.DublinCore;
                case "provenance":
                    return (int)Enums.MetadataFormats.Provenance;

                // TODO: Add format here

                default: return (int)Enums.MetadataFormats.None;
            }
        }

        public static Enums.MetadataFormats Int2Format(int mdFormat)
        {
            foreach (var format in FormatList.List)
            {
                if (IsInFormat(mdFormat, format.FormatNum))
                {
                    return format.FormatNum;
                }
            }

            return Enums.MetadataFormats.None;
        }

        public static bool IsInFormat(int mdFormat, Enums.MetadataFormats format)
        {
            return IsInFormat(mdFormat, (int)format);
        }
        public static bool IsInFormat(int mdFormat, string metadataPrefix)
        {
            return IsInFormat(mdFormat, Prefix2Int(metadataPrefix));
        }
        public static bool IsInFormat(int mdFormat, int formatNum)
        {
            return (mdFormat & formatNum) != 0;
        }

        public static IEnumerable<OAIMetadataFormat> GetAllFormatsFromInt(int mdFormat)
        {
            foreach (var item in FormatList.List)
            {
                if (item.IsForList && IsInFormat(mdFormat, item.FormatNum))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<Metadata> GetAllMetadatasWithFormat(IEnumerable<Metadata> metaList, Enums.MetadataFormats format)
        {
            if (metaList == null)
            {
                yield break;
            }

            foreach (var item in metaList)
            {
                if (IsInFormat(item.MdFormat, format))
                {
                    yield return item;
                }
            }
        }

        public static string GetNamespace(string metadataPrefix)
        {
            if (string.IsNullOrEmpty(metadataPrefix))
            {
                return null;
            }

            foreach (var item in List)
            {
                if (item.Prefix == metadataPrefix)
                {
                    return item.Namespace;
                }
            }

            return null;
        }
    }
}