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
using FederatedSearch.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace FederatedSearch.API.Internal
{
    public class DeDuplicate
    {
        public static void Records(
            List<RecordQueryResult> records,
            OaiPmhContext context,
            Enums.DeDuplication deDup)
        {
            switch (deDup)
            {
                case Enums.DeDuplication.AddDuplicate:
                    break;
                case Enums.DeDuplication.UpdateOriginal:
                    {
                        /* get identifiers from XML records */
                        /*
var recIdentifiers = records.Select(re => re.OAIRecord.OAI_Identifier);

/* get records with same identifier */
                        /*
var recsToModify = (from rec in context.OAIRecord
     from recIdentifier in recIdentifiers
     where rec.OAI_Identifier == recIdentifier
     select rec).ToList();

(from recToModify in recsToModify
from rec in records
where rec.OAIRecord.OAI_Identifier == recToModify.OAI_Identifier
select new RecRecQueryResult
{
recToModify = recToModify,
rec = rec
}).ToList().ForEach((r) =>
{
RecordQueryResult tempRec = r.rec;
records.Remove(r.rec);
if (tempRec.Metadata != null)
{
//tempRec.Metadata.Id = r.recToModify.Metadata == null ? 0 : r.recToModify.Metadata.Id;
//tempRec.OAIRecord.DCId = tempRec.Metadata.Id;
}
tempRec.OAIRecord.Id = r.recToModify.Id;
//tempRec.OAIRecord.DublinCore = tempRec.Metadata;
r.recToModify = tempRec.OAIRecord;
context.Entry(r.recToModify).State = EntityState.Modified;
});*/
                    }
                    break;
                case Enums.DeDuplication.Skip:
                    {
                        var recIdentifiers = records.Select(re => re.Header.OAI_Identifier).ToList();

                        using (var cmd = context.Database.Connection.CreateCommand())
                        {
                            cmd.CommandText = "DeDuplicateSkip";
                            cmd.CommandType = CommandType.StoredProcedure;

                            /* pass whole list as parameter to SP */
                            var idTable = new DataTable();
                            idTable.Columns.Add("Item", typeof(string));

                            recIdentifiers.ForEach(r => idTable.Rows.Add(r)); /* fill table with ids */
                            cmd.Parameters.Add(new SqlParameter("@idList", SqlDbType.Structured)
                                                    {
                                                        TypeName = "dbo.RecIdList",
                                                        Value = idTable
                                                    });
                            cmd.Parameters.Add(new SqlParameter("@objectType", Enums.ObjectType.OAIRecord));
                            cmd.Parameters.Add(new SqlParameter("@metadataType", Enums.MetadataType.About));
                            cmd.Parameters.Add(new SqlParameter("@provenanceNum", (byte)Enums.MetadataFormats.Provenance));

                            try
                            {
                                recIdentifiers.Clear();
                                context.Database.Connection.Open();
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            var item = reader.GetString(0);
                                            recIdentifiers.Add(reader.GetString(0));
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                context.Database.Connection.Close();
                            }
                        }

                        try
                        {
                            records.RemoveAll(r => recIdentifiers.Contains(r.Header.OAI_Identifier));
                        }
                        catch (Exception e)
                        {
                            string msg = e.Message;
                        }

                        /* we have to split bigger lists because the query becomes too big and ef crashes */
                        /*foreach (var splitList in Helper.SplitList<string>(recIdentifiers))
                        {
                            /* check record identifiers */
                        /*var recsToSkip = (from rec in context.Header
                                          from recIdentifier in splitList
                                          where rec.OAI_Identifier == recIdentifier
                                          select rec.OAI_Identifier).ToList();
                        /* check provenance identifiers */
                        /*recsToSkip.AddRange((from rec in context.Header
                                             join om in context.ObjectMetadata on rec.HeaderId equals om.ObjectId
                                             join md in context.Metadata on om.MetadataId equals md.MetadataId
                                             where om.ObjectType == Enums.ObjectType.OAIRecord
                                             where om.MetadataType == Enums.MetadataType.About
                                             where (md.MdFormat & (byte)Enums.MetadataFormats.Provenance) != 0
                                             from ri in splitList
                                             where md.Identifier == ri
                                             select md.Identifier).ToList());
                        try
                        {
                            records.RemoveAll(r => recsToSkip.Contains(r.Header.OAI_Identifier));
                        }
                        catch (Exception e)
                        {
                            string msg = e.Message;
                            continue;
                        }
                    }*/
                    }
                    break;
                default: break;
            }
        }
    }
}