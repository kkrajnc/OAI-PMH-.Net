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
using FederatedSearch.API.MdFormats;
using FederatedSearch.API.Common;
using FederatedSearch.Models;

namespace FederatedSearch.API.Internal
{
    public class RecordQueryResult
    {
        public RecordQueryResult()
        { }

        public RecordQueryResult(Header header, Metadata metadata)
        {
            this.Header = header;
            this.Metadata = metadata;
        }
        public RecordQueryResult(Header header, Metadata metadata, IList<Metadata> about)
        {
            this.Header = header;
            this.Metadata = metadata;
            this.About = about == null ? null : about.ToList();
        }
        public RecordQueryResult(Header header, Metadata metadata, IEnumerable<Metadata> about)
        {
            this.Header = header;
            this.Metadata = metadata;
            this.About = about == null ? null : about.ToList();
        }

        public Header Header { get; set; }
        public Metadata Metadata { get; set; }
        public List<Metadata> About { get; set; }

        public static void AddRecordToDatabase(
            RecordQueryResult record, 
            OaiPmhContext context, 
            OAIDataProvider dp, 
            string metadataPrefix, 
            DateTime harvestDate, 
            bool addProvenance, 
            bool createNewIdentifier,
            string identifierBase,
            bool isHarvestDateTime)
        {
            if (addProvenance)
            {
                record.About.Add(Provenance.NewMeta(harvestDate,
                    isHarvestDateTime,
                    createNewIdentifier,
                    dp.BaseURL,
                    record.Header.OAI_Identifier,
                    record.Header.Datestamp.HasValue ? record.Header.Datestamp.Value : DateTime.MinValue,
                    record.Header.IsDatestampDateTime,
                    FormatList.GetNamespace(metadataPrefix)));
            }

            /* add header */
            Header.AddRecHeaderToDatabase(
                context,
                record.Header,
                dp,
                createNewIdentifier,
                identifierBase);

            /* add metadata */
            DbQueries.AddRecMetadataToDatabase(
                context,
                record.Header.HeaderId,
                record.Metadata);

            /* add about */
            DbQueries.AddRecAboutToDatabase(
                context,
                record.Header.HeaderId,
                record.About);
        }
    }
}