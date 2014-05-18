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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace FederatedSearch.Models
{
    public partial class OAIDataProvider
    {
        public static OAIDataProvider Decode(XElement root)
        {
            if (root == null)
            {
                return null;
            }
            return new OAIDataProvider()
            {
                RepositoryName = MlDecode.Element(root, MlNamespaces.oaiNs + "repositoryName"),
                BaseURL = MlDecode.Element(root, MlNamespaces.oaiNs + "baseURL"),
                ProtocolVersion = MlDecode.Element(root, MlNamespaces.oaiNs + "protocolVersion"),
                AdminEmail = MlDecode.Element(root, MlNamespaces.oaiNs + "adminEmail"),
                EarliestDatestamp = MlDecode.SafeDateTime(root.Element(MlNamespaces.oaiNs + "earliestDatestamp")),
                DeletedRecord = MlDecode.Element(root, MlNamespaces.oaiNs + "deletedRecord"),
                Granularity = MlDecode.Element(root, MlNamespaces.oaiNs + "granularity"),
                Compression = MlDecode.Element(root, MlNamespaces.oaiNs + "compression")
            };
        }

        public static XElement Encode(OAIDataProvider dataProvider, string granularity)
        {
            return new XElement(MlNamespaces.oaiNs + "dataProvider",
                /* content */
                MlEncode.Element(MlNamespaces.oaiNs + "repositoryName", dataProvider.RepositoryName),
                MlEncode.Element(MlNamespaces.oaiNs + "baseURL", dataProvider.BaseURL),
                MlEncode.Element(MlNamespaces.oaiNs + "protocolVersion", dataProvider.ProtocolVersion),
                MlEncode.Element(MlNamespaces.oaiNs + "adminEmail", dataProvider.AdminEmail),
                !dataProvider.EarliestDatestamp.HasValue ? null
                        : new XElement(MlNamespaces.oaiNs + "earliestDatestamp",
                            dataProvider.EarliestDatestamp.Value.ToUniversalTime().ToString(granularity)),
                MlEncode.Element(MlNamespaces.oaiNs + "deletedRecord", dataProvider.DeletedRecord),
                MlEncode.Element(MlNamespaces.oaiNs + "granularity", dataProvider.Granularity),
                MlEncode.Element(MlNamespaces.oaiNs + "compression", dataProvider.Compression),
                !dataProvider.LastHarvesting.HasValue ? null
                        : new XElement(MlNamespaces.oaiNs + "lastHarvesting",
                            dataProvider.LastHarvesting.Value.ToUniversalTime().ToString(granularity))
                );
        }
    }
}