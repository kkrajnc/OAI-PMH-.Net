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

namespace FederatedSearch.API.Common
{
    public class MlNamespaces
    {
        /* OAI */
        public static XNamespace oaiNs = "http://www.openarchives.org/OAI/2.0/";
        public static XNamespace oaiXsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static XNamespace oaiSchemaLocation = "http://www.openarchives.org/OAI/2.0/ " + 
                                                     "http://www.openarchives.org/OAI/2.0/OAI-PMH.xsd";


        /* Dublin Core */
        public static XNamespace oaiDc = "http://www.openarchives.org/OAI/2.0/oai_dc/";
        public static XNamespace dcNs = "http://purl.org/dc/elements/1.1/";
        public static XNamespace dcXsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static XNamespace dcSchemaLocation = "http://www.openarchives.org/OAI/2.0/oai_dc/ " + 
                                                    "http://www.openarchives.org/OAI/2.0/oai_dc.xsd";


        /* Provenance */
        public static XNamespace provNs = "http://www.openarchives.org/OAI/2.0/provenance";
        public static XNamespace provXsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static XNamespace provSchemaLocation = "http://www.openarchives.org/OAI/2.0/provenance " + 
                                                      "http://www.openarchives.org/OAI/2.0/provenance.xsd";

        /* TODO: Add format here */

    }
}