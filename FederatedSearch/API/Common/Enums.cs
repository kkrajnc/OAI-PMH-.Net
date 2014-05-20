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

namespace FederatedSearch.API.Common
{
    public class Enums
    {
        /* What to do with duplicates when harvesting */
        public enum DeDuplication
        {
            AddDuplicate,

            UpdateOriginal,

            Skip
        }

        /* Support for deleted records */
        public class DeletedRecords
        {
            /* No information about deletion is kept */
            public const string No = "no";

            /* Informations about deletion are maintained with no time limit */
            public const string Persistent = "persistent";

            /* No guaratee that a list of deletions is maintained persistently or consistently */
            public const string Transient = "transient";
        }

        public class Granularity
        {
            /* UTC year format */
            public const string Year = "yyyy";

            /* UTC year and month format */
            public const string YearAndMonth = "yyyy'-'MM";

            /* UTC date format */
            public const string Date = "yyyy'-'MM'-'dd";

            /* UTC date time format */
            public const string DateTime = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'";
        }

        public class ObjectType
        {
            public static byte OAIRecord = 0;
            public static byte OAISet = 1;
        }

        public class MetadataType
        {
            public static byte Metadata = 0;
            public static byte About = 1;
        }

        /* List of supported metadata formats 2^* values */
        public enum MetadataFormats
        {
            None = 0,
            DublinCore = 1,
            Provenance = 2
        }
    }
}