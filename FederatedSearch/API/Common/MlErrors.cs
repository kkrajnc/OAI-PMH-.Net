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

namespace FederatedSearch.API.Common
{
    public class MlErrors
    {
        public static XElement badArgument = MlEncode.Error("badArgument",
                "The request includes illegal arguments, is missing required arguments, " +
                "includes a repeated argument, or values for arguments have an illegal syntax.");

        /* Required */
        public static XElement badMetadataArgument = MlEncode.Error("badArgument",
            "The request does not include a 'metadataPrefix' value which is required");
        public static XElement badIdentifierArgument = MlEncode.Error("badArgument",
            "The request does not include a 'identifier' attribute which is required");

        /* Not Allowed */
        public static XElement badFromArgumentNotAllowed = MlEncode.Error("badArgument",
            "The request includes a 'from' value that is not allowed.");
        public static XElement badUntilArgumentNotAllowed = MlEncode.Error("badArgument",
            "The request includes a 'until' value that is not allowed.");
        public static XElement badMetadataArgumentNotAllowed = MlEncode.Error("badArgument",
            "The request includes a 'metadataPrefix' value that is not allowed.");
        public static XElement badSetArgumentNotAllowed = MlEncode.Error("badArgument",
            "The request includes a 'set' value that is not allowed.");
        public static XElement badResumptionArgumentNotAllowed = MlEncode.Error("badArgument",
            "The request includes a 'resumptionToken' value that is not allowed");
        public static XElement badIdentifierArgumentNotAllowed = MlEncode.Error("badArgument",
            "The request includes a 'identifier' value that is not allowed.");

        /* Illegal syntax */
        public static XElement badFromArgument = MlEncode.Error("badArgument",
            "The request includes a 'from' value that has an illegal syntax / granularity.");
        public static XElement badUntilArgument = MlEncode.Error("badArgument",
            "The request includes a 'until' value that has an illegal syntax / granularity.");

        /* Resumption */
        public static XElement badResumptionArgumentOnly = MlEncode.Error("badArgument",
            "The request includes a 'resumptionToken' value which must be the only " +
            "argument besides the verb in query.");
        public static XElement badResumptionArgument = MlEncode.Error("badResumptionToken",
            "The value of the 'resumptionToken' argument is invalid or expired.");

        /* Verb */
        public static XElement badVerbArgument = MlEncode.Error("badArgument",
            "Wrong function was called with this 'verb' argument. Please contact admin.");
        public static XElement badVerb = MlEncode.Error("badVerb",
            "Value of the 'verb' argument is not a legal OAI-PMH verb, the verb argument is missing, " +
            "or the verb argument is repeated.");
        public static XElement badVerbMissing = MlEncode.Error("badVerb",
            "The request does not provide any 'verb'.");

        /* Metadata Prefix */
        public static XElement cannotDisseminateFormat = MlEncode.Error("cannotDisseminateFormat",
            "The metadata format identified by the value given for the 'metadataPrefix' argument " +
            "is not supported by the repository.");
        public static XElement cannotDisseminateRecordFormat = MlEncode.Error("cannotDisseminateFormat",
            "The metadata format identified by the value given for the 'metadataPrefix' argument " +
            "is not supported by the item.");

        /* Set */
        public static XElement noSetHierarchy = MlEncode.Error("noSetHierarchy",
            "The repository does not support sets.");

        /* Other */
        public static XElement idDoesNotExist = MlEncode.Error("idDoesNotExist",
            "The value of the 'identifier' argument is unknown or illegal in this repository.");

        public static XElement noRecordsMatch = MlEncode.Error("noRecordsMatch",
            "The combination of the values of the 'from', 'until', 'set' and 'metadataPrefix' arguments " +
            "results in an empty list.");

        public static XElement noMetadataFormats = MlEncode.Error("noMetadataFormats",
            "There are no metadata formats available for the specified item.");

        public static XElement badFromAndUntilArgument = MlEncode.Error("badArgument",
            "The 'from' argument must be less than or equal to the 'until' argument.");
    }
}