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

using FederatedSearch.API;
using FederatedSearch.API.Common;
using FederatedSearch.API.Internal;
using FederatedSearch.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FederatedSearch.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var baseLocalUrl = Common.GetBaseApiUrl(this);
            ViewBag.DataProviders = await OaiApiRestService.GetDataProviders(baseLocalUrl) ?? new List<OAIDataProvider>();
            return View();
        }

        [HttpPost]
        public ActionResult Index(FormCollection form)
        {
            string BaseURL = form["BaseURL"];
            string UrlQuery = form["UrlQuery"];
            if (string.IsNullOrEmpty(BaseURL) || string.IsNullOrEmpty(UrlQuery))
            {
                return null;
            }

            string apiUrl = Common.GetBaseApiUrl(this) + "api/oai?" + UrlQuery;
            string dataProviderUrl = BaseURL + "?" + UrlQuery;
            Stopwatch stopWatch = new Stopwatch();
            string apiContent;
            string dataProviderContent;
            TimeSpan apiTime;
            TimeSpan dataProviderTime;

            using (HttpClient client = new HttpClient())
            {
                stopWatch.Start();
                dataProviderContent = client.GetStringAsync(dataProviderUrl).Result;
                stopWatch.Stop();
                dataProviderTime = stopWatch.Elapsed;

                stopWatch.Reset();

                stopWatch.Start();
                apiContent = client.GetStringAsync(apiUrl).Result;
                System.Threading.Thread.Sleep(3000);
                stopWatch.Stop();
                apiTime = stopWatch.Elapsed;
            }
            return Json(new
            {
                ApiResult = apiTime.TotalSeconds,
                DataProviderResult = dataProviderTime.TotalSeconds,
                Ratio = (Math.Round(dataProviderTime.TotalMilliseconds / apiTime.TotalMilliseconds, 2, MidpointRounding.AwayFromZero)).ToString()
            });
        }

        public ActionResult Search(string search = null)
        {
            return View(search as object);
        }

        public async Task<ActionResult> Metadata(
            string id,
            int ipp = 0,
            string search = null,
            int page = 0)
        {
            string baseUrl = Common.GetBaseApiUrl(this);
            if (!string.IsNullOrEmpty(id) && string.IsNullOrEmpty(search))
            {
                return View("MetadataItem", await OaiApiRestService.GetMetadata(baseUrl, id));
            }
            else
            {
                /* add query params */
                ViewBag.ItemsPerPage = ipp;
                ViewBag.Search = search;
                ViewBag.PageNum = page;

                /* get metadata and result count */
                MetaList ml = await OaiApiRestService.GetMetadata(baseUrl, ipp, search, page) ?? new MetaList();
                ViewBag.ResultCount = ml.ResultCount;

                return View("MetadataList", ml.List);
            }
        }

        public ActionResult Test()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Test(string id, string regex, string value)
        {
            switch (id)
            {
                case "regex":
                    string result = string.Empty;
                    foreach (Match match in new Regex(regex, RegexOptions.Compiled).Matches(value))
                    {
                        result += match.Value + ", ";
                    }

                    return Json(new
                    {
                        result = result
                    });

                case "combine":
                    Uri tmpUri, baseUri = new Uri(regex);
                    if (Uri.TryCreate(baseUri, value, out tmpUri))
                    {
                        return Json(new
                        {
                            result = tmpUri.ToString()
                        });
                    }
                    break;
            }
            return null;
        }
    }
}
