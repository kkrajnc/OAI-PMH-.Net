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
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace FederatedSearch.Controllers
{
    public static class MenuLinkExtension
    {
        public static MvcHtmlString MenuLink(
            this HtmlHelper htmlHelper,
            string itemText,
            string actionName,
            string controllerName,
            object routeValues = null,
            MvcHtmlString[] childElements = null)
        {
            var startLi = new TagBuilder("li");
            var innerLink = new TagBuilder("a");

            if (childElements != null && childElements.Length > 0)
            {
                /* <li> */
                startLi.AddCssClass("dropdown");
                if (controllerName == htmlHelper.ViewContext.RouteData.GetRequiredString("controller"))
                {
                    startLi.AddCssClass("active");
                    startLi.AddCssClass("usedDrop");
                }
                /* <a> */
                innerLink.MergeAttribute("data-toggle", "dropdown");
                innerLink.AddCssClass("dropdown-toggle");

                UrlHelper urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext, htmlHelper.RouteCollection);
                if (routeValues == null)
                {
                    innerLink.MergeAttribute("href", urlHelper.Action(actionName, controllerName, new { id = "" }));
                }
                else
                {
                    innerLink.MergeAttribute("href", urlHelper.Action(actionName, controllerName, routeValues));
                }

                innerLink.InnerHtml = itemText + " <b class=\"caret\"></b>";
                /* <ul> */
                var innerUl = new TagBuilder("ul");
                innerUl.MergeAttribute("role", "menu");
                innerUl.AddCssClass("dropdown-menu");
                foreach (var item in childElements)
                {
                    innerUl.InnerHtml += item.ToString() + "\n";
                }

                startLi.InnerHtml = innerLink.ToString() + "\n" + innerUl.ToString();
                return new MvcHtmlString(startLi.ToString());
            }
            else
            {
                string action = string.Empty;
                UrlHelper urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext, htmlHelper.RouteCollection);
                if (routeValues == null)
                {
                    action = urlHelper.Action(actionName, controllerName, new { id = "" });
                }
                else
                {
                    action = urlHelper.Action(actionName, controllerName, routeValues);
                }
                innerLink.MergeAttribute("href", action);
                innerLink.SetInnerText(itemText);

                if(action == htmlHelper.ViewContext.RequestContext.HttpContext.Request.Url.AbsolutePath)
                {
                    startLi.AddCssClass("active");
                }

                startLi.InnerHtml = innerLink.ToString();
                return new MvcHtmlString(startLi.ToString());
            }
        }

        public static MvcHtmlString MetadataPreferedTitle(
            this HtmlHelper htmlHelper, 
            string value, 
            string lang, 
            bool addAllWithoutAttribute)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new MvcHtmlString("Title is missing!");
            }

            var title = MlEncode.LimitElemenetsOnLang(value, lang, addAllWithoutAttribute).FirstOrDefault();
            if (string.IsNullOrEmpty(title))
            {
                title = MlEncode.Element("el", value).FirstOrDefault().Value;
                if (string.IsNullOrEmpty(title))
                {
                    title = "Title is missing!";
                }
            }
            return new MvcHtmlString(title);
        }

        public static MvcHtmlString MetadataSubjectList(
            this HtmlHelper htmlHelper,
            string value,
            string lang)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var subject in MlEncode.LimitElemenetsOnLang(value, lang, true))
            {
                if (!string.IsNullOrEmpty(subject) && !subject.StartsWith("info:"))
                {
                    sb.Append(htmlHelper.ActionLink(subject, "Metadata", new { search = subject }));
                    sb.Append(", ");
                }
            }
            var list = sb.ToString();
            return new MvcHtmlString(list.Substring(0, list.Length - 2));
        }

        public static MvcHtmlString MetadataReverseNames(this HtmlHelper htmlHelper, string value)
        {
            return new MvcHtmlString(Common.EnumerableToString(Common.ReverseNames(value)));
        }

        public static MvcHtmlString MetadataElementSplit(this HtmlHelper htmlHelper, string value)
        {
            return new MvcHtmlString(Common.EnumerableToString(MlEncode.ListElementValues(value)));
        }

        public static MvcHtmlString MetadataElementSplit(this HtmlHelper htmlHelper, string value, string lang)
        {
            return new MvcHtmlString(Common.EnumerableToString(MlEncode.LimitElemenetsOnLang(value, lang, false)));
        }

        public static MvcHtmlString GetStackedList(this HtmlHelper htmlHelper, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var sb = new StringBuilder();
                var items = MlEncode.Element("item", value).ToList();

                for (int i = 0; i < items.Count(); i++)
                {
                    if (!string.IsNullOrEmpty(items[i].Value))
                    {
                        sb.Append(items[i].Value);
                        if ((i + 1) == items.Count())
                        {
                            sb.Append("<br />");
                        }
                    }
                }

                return new MvcHtmlString(sb.ToString());
            }

            return new MvcHtmlString("/");
        }
    }
}