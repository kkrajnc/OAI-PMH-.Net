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
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using FederatedSearch.Models;
using FederatedSearch.API.Common;
using System.Data.SqlClient;

namespace FederatedSearch.Controllers
{
    public class oaiMetadataController : ApiController
    {
        private OaiPmhContext db = new OaiPmhContext();

        private IQueryable<Metadata> GetQuery(string identifier)
        {
            return (from h in db.Header
                    join om in db.ObjectMetadata on h.HeaderId equals om.ObjectId
                    join md in db.Metadata on om.MetadataId equals md.MetadataId
                    where om.ObjectType == Enums.ObjectType.OAIRecord
                    where om.MetadataType == Enums.MetadataType.Metadata
                    where (md.MdFormat & (byte)Enums.MetadataFormats.DublinCore) != 0
                    where h.OAI_Identifier == identifier
                    orderby h.Datestamp
                    select md);
        }
        private IQueryable<MetaSearchResult> GetQuery()
        {
            return (from h in db.Header
                    join om in db.ObjectMetadata on h.HeaderId equals om.ObjectId
                    join md in db.Metadata on om.MetadataId equals md.MetadataId
                    where om.ObjectType == Enums.ObjectType.OAIRecord
                    where om.MetadataType == Enums.MetadataType.Metadata
                    where (md.MdFormat & (byte)Enums.MetadataFormats.DublinCore) != 0
                    orderby h.Datestamp
                    select new MetaSearchResult()
                                {
                                    Creator = md.Creator,
                                    Date = md.Date,
                                    Datestamp = h.Datestamp,
                                    HeaderId = h.HeaderId,
                                    MetadataId = md.MetadataId,
                                    OAI_Identifier = h.OAI_Identifier,
                                    Subject = md.Subject,
                                    Title = md.Title
                                });
        }

        // GET api/Metadata?ipp=5&page=2
        public HttpResponseMessage GetMetadata(string id = null, int ipp = 0, string search = null, int page = 0)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Metadata metadata = (Metadata)GetQuery(id).FirstOrDefault();
                if (metadata == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }

                return Common.JsonResponse(metadata);
            }
            else
            {
                List<MetaSearchResult> metaList;
                int listCounter = 0;
                page -= 1;
                ipp = ipp <= 0 ? 10 : ipp;

                if (string.IsNullOrEmpty(search))
                {
                    listCounter = GetQuery().Count();
                    if (page <= 0)
                    {
                        metaList = GetQuery().Take(ipp).ToList();
                    }
                    else
                    {
                        metaList = GetQuery().Skip(ipp * page).Take(ipp).ToList();
                    }
                }
                else
                {
                    search = SplitKeyWord(search);
                    /* SP params */
                    var searchParam = new SqlParameter("searchStr", search);
                    var objectTypeParam = new SqlParameter("objectType", Enums.ObjectType.OAIRecord);
                    var metaTypeParam = new SqlParameter("metaType", Enums.MetadataType.Metadata);
                    var metaFormatParam = new SqlParameter("metaFormat", Enums.MetadataFormats.DublinCore);
                    var skipParam = new SqlParameter("skip", page <= 0 ? 0 : ipp * page);
                    var takeParam = new SqlParameter("take", ipp);
                    var resultCount = new SqlParameter("resultCount", Convert.ToInt32(0));
                    resultCount.Direction = ParameterDirection.Output;

                    /* Execute SP */
                    var results = db.Database.SqlQuery<MetaSearchResult>(
                        "MetaFts @searchStr, @objectType, @metaType, @metaFormat, @skip, @take, @resultCount OUT",
                          searchParam,
                          objectTypeParam,
                          metaTypeParam,
                          metaFormatParam,
                          skipParam,
                          takeParam,
                          resultCount);

                    metaList = results.ToList();
                    listCounter = metaList.Last().HeaderId; /* we saved the number of items in last row */
                    metaList.Remove(metaList.Last());
                }

                return Common.JsonResponse(new { ResultCount = listCounter, List = metaList });
            }
        }

        private string SplitKeyWord(string value)
        {

            string result = "";
            foreach (string part in value.Split(' '))
            {
                if (!string.IsNullOrEmpty(part.Trim()))
                {
                    result += "\"" + part + "*\" AND ";
                }
            }
            if ((result.Length > 0))
            {
                result = result.Substring(0, result.LastIndexOf("AND"));
            }

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}