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
using FederatedSearch.API;
using System.Xml.Linq;
using System.Text;

namespace FederatedSearch.Controllers
{
    public class oaiController : ApiController
    {
        public HttpResponseMessage Identify()
        {
            return Common.XDocResponse(DataProvider.CheckAttributes("Identify"));
        }

        public HttpResponseMessage CheckAttributes(
            string verb,
            string from,
            string until,
            string metadataPrefix,
            string set,
            string resumptionToken,
            string identifier)
        {
            return Common.XDocResponse(DataProvider.CheckAttributes(
                        verb,
                        from,
                        until,
                        metadataPrefix,
                        set,
                        resumptionToken,
                        identifier));
        }

        // GET api/oai
        public HttpResponseMessage Get()
        {
            return Identify();
        }

        // GET api/oai
        public HttpResponseMessage Get(
            string verb,
            string from = null,
            string until = null,
            string metadataPrefix = null,
            string set = null,
            string resumptionToken = null,
            string identifier = null)
        {
            return CheckAttributes(
                        verb,
                        from,
                        until,
                        metadataPrefix,
                        set,
                        resumptionToken,
                        identifier);
        }

        // POST api/oai
        public HttpResponseMessage Post()
        {
            return Identify();
        }

        // POST api/oai
        public HttpResponseMessage Post(
            string verb,
            string from = null,
            string until = null,
            string metadataPrefix = null,
            string set = null,
            string resumptionToken = null,
            string identifier = null)
        {
            return CheckAttributes(
                        verb,
                        from,
                        until,
                        metadataPrefix,
                        set,
                        resumptionToken,
                        identifier);
        }
    }
}