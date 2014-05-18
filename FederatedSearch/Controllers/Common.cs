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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace FederatedSearch.Controllers
{
    public class Common
    {
        /* Xml Responses */
        public static HttpResponseMessage XmlResponse(string xmlContent)
        {
            return new HttpResponseMessage
            {
                Content = new StringContent(
                    xmlContent,
                    Encoding.UTF8,
                    "application/xml")
            };
        }

        public static string XDocToString(XElement element)
        {
            return XDocToString(new XDocument(element));
        }
        public static string XDocToString(XDocument xml)
        {
            return (xml.Declaration == null ? new XDeclaration("1.0", "utf-8", "no").ToString() : xml.Declaration.ToString()) +
                Environment.NewLine +
                xml.ToString();
        }

        public static HttpResponseMessage XdocNullResponse()
        {
            return XDocResponse(new XDocument(new XElement("error", "function result is null")));
        }

        public static HttpResponseMessage XDocResponse(XElement element)
        {
            return XDocResponse(new XDocument(element));
        }
        public static HttpResponseMessage XDocResponse(XDocument xml)
        {
            return XmlResponse(XDocToString(xml));
        }



        /* Json Responses */
        public static HttpResponseMessage JsonResponse(object content)
        {
            return new HttpResponseMessage
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(content, Formatting.None),
                    Encoding.UTF8,
                    "application/json")
            };
        }

        public static HttpResponseMessage JsonNullResponse()
        {
            return JsonResponse(new { error = "function result is null" });
        }

        public static HttpResponseMessage JsonErrorResponse(string message)
        {
            return JsonResponse(new { error = message });
        }

        /* Xml / Metadata operations */

        public static IEnumerable<string> ReverseNames(string value)
        {
            foreach (var xElName in MlEncode.Element("el", value))
            {
                string normalName = string.Empty;
                string[] splitNames = xElName.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = splitNames.Length - 1; i >= 0; i--)
                {
                    normalName += splitNames[i].Trim() + (i == 0 ? "" : " ");
                }
                yield return normalName;
            }
        }

        public static string EnumerableToString(IEnumerable<string> list)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in list)
            {
                sb.Append(item);
                sb.Append(", ");
            }

            if (sb.Length > 2)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            return sb.ToString();
        }

        /* ---------------------------------------------------------*/
        public static string GetBaseUrl(Controller controller)
        {
            return GetBaseUrl(controller.HttpContext);
        }
        public static string GetBaseUrl(HttpContextBase httpContext)
        {
            var pathAndQuery = httpContext.Request.Url.PathAndQuery;
            return httpContext.Request.Url.AbsoluteUri.Replace(pathAndQuery, "/");
        }
    }
}