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
using FederatedSearch.API.MdFormats;
using FederatedSearch.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace FederatedSearch.API.Internal
{
    public class DbQueries
    {
        public static IQueryable<Header> GetHeader(
            OaiPmhContext context,
            string identifier)
        {
            return from rec in context.Header
                   where rec.OAI_Identifier == identifier
                   select rec;
        }

        public static void AddMetadataToDatabase(OaiPmhContext context, int objId, byte objType, byte metaType, Metadata metadata)
        {
            context.Metadata.Add(metadata);
            context.SaveChanges();
            context.ObjectMetadata.Add(new ObjectMetadata()
            {
                ObjectId = objId,
                ObjectType = objType,
                MetadataType = metaType,
                MetadataId = metadata.MetadataId
            });
        }
            
        public static void AddToDatabase(OaiPmhContext context, int objId, byte objType, byte metaType, Metadata metadata)
        {
            switch (FormatList.Int2Format(metadata.MdFormat))
            {
                case Enums.MetadataFormats.DublinCore:
                    DublinCore.AddToDatabase(context, objId, objType, metaType, metadata);
                    break;
                case Enums.MetadataFormats.Provenance:
                    Provenance.AddToDatabase(context, objId, objType, metaType, metadata);
                    break;

                // TODO: Add format here

                case Enums.MetadataFormats.None:
                default:
                    break;
            }
        }

        public static void AddRecMetadataToDatabase(OaiPmhContext context, int recId, Metadata metadata)
        {
            if (context == null || recId == 0 || metadata == null)
            {
                return;
            }

            AddToDatabase(context, recId, Enums.ObjectType.OAIRecord, Enums.MetadataType.Metadata, metadata);
        }

        public static void AddRecAboutToDatabase(OaiPmhContext context, int recId, List<Metadata> about)
        {
            if (context == null || recId == 0 || about == null)
            {
                return;
            }

            foreach (var item in about)
            {
                if (item != null)
                {
                    AddToDatabase(context, recId, Enums.ObjectType.OAIRecord, Enums.MetadataType.About, item);
                }
            }
        }
    }
}