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

using FederatedSearch.API.Internal;
using FederatedSearch.API.MdFormats;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace FederatedSearch.API.Common
{
    public class FileHarvester
    {
        public static string GetOrCreateFileName(HttpWebResponse response, bool keepFileName, string defaultValue)
        {
            string fileName = null;
            if (keepFileName && response.Headers.AllKeys.Contains("Content-Disposition"))
            {
                string contentDisposition = response.Headers.GetValues("Content-Disposition").FirstOrDefault();
                fileName = string.IsNullOrEmpty(contentDisposition) ?
                            null : new ContentDisposition(contentDisposition).FileName;
            }

            return string.IsNullOrEmpty(fileName) ? defaultValue : fileName;
        }

        public static string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            return new Regex("[^a-zA-Z0-9_.-]+", RegexOptions.Compiled).Replace(fileName, "");
        }

        public static string CreateUniqueFileName(string basePath, string fileName, string fileExtension)
        {
            string filePath = string.Empty;
            /* create file path */
            filePath = basePath +
                       (string.IsNullOrEmpty(fileName) ? CleanFileName(Helper.CreateGuid()) : CleanFileName(fileName));

            if (!filePath.EndsWith(fileExtension, true, System.Globalization.CultureInfo.InvariantCulture))
            {
                filePath += fileExtension;
            }

            /* create unique file name */
            if (!Properties.overwriteHarvestedFiles && File.Exists(filePath))
            {
                int extIndex = filePath.LastIndexOf('.');
                string fileExt = filePath.Substring(extIndex);
                filePath = filePath.Substring(0, extIndex);

                int counter = 0;
                do
                {
                    counter += 1;
                } while (File.Exists(filePath + "-" + counter + fileExt));
                filePath = filePath + "-" + counter + fileExt;
            }

            return filePath;
        }

        public static string GetResponseContentType(HttpWebResponse response)
        {
            if (response.Headers.AllKeys.Contains("Content-Type"))
            {
                return response.Headers.GetValues("Content-Type").FirstOrDefault();
            }

            return string.Empty;
        }

        public static string GetExtension(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return string.Empty;
            }
            var key = Registry.ClassesRoot.OpenSubKey(@"MIME\Database\Content Type\" + format, false);
            var extensionValue = key != null ? key.GetValue("Extension", null) : null;
            return extensionValue != null ? extensionValue.ToString() : string.Empty;
        }

        public static bool DownloadFile(string filePath, string uri)
        {
            var response = GetResponse(uri);

            return DownloadFile(filePath, response);
        }
        public static bool DownloadFile(string filePath, HttpWebResponse response)
        {
            if (string.IsNullOrEmpty(filePath) || response == null)
            {
                return false;
            }

            if (Properties.limitHarvestedFileTypes && 
                Properties.allowedMimeTypesList.Contains(GetResponseContentType(response)))
            {
                return false;
            }

            try
            {
                using (Stream contentStream = response.GetResponseStream(),
                        stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, true))
                {
                    contentStream.CopyTo(stream);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public static HttpWebResponse GetResponse(string uri, CookieContainer cookies = null)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(new Uri(uri));
                request.Method = "GET";
                request.CookieContainer = cookies;
                return (HttpWebResponse)request.GetResponse();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static HttpWebResponse PostResponse(string uri, string content, CookieContainer cookies = null)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(new Uri(uri));
                request.Method = "POST";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = content.Length;
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:27.0) Gecko/20100101 Firefox/27.0";
                request.Referer = uri;
                request.KeepAlive = true;
                request.CookieContainer = cookies;

                /* write key to the stream */
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(content);
                }

                return (HttpWebResponse)request.GetResponse();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string FindInPage(HttpWebResponse response, string lineRegex, string valueRegex)
        {
            bool skipLineCheck = string.IsNullOrEmpty(lineRegex);
            string line = string.Empty;
            using (var responseStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                while ((line = responseStream.ReadLine()) != null)
                {
                    if (skipLineCheck || Regex.IsMatch(line, lineRegex, RegexOptions.Compiled))
                    {
                        return new Regex(valueRegex, RegexOptions.Compiled).Match(line).Value;
                    }
                }
            }

            return string.Empty;
        }

        public static IEnumerable<FileProperties> GetAllSources(RecordQueryResult record, string sourceProperty)
        {
            if (record == null || string.IsNullOrEmpty(sourceProperty))
            {
                yield break;
            }

            FileProperties tempProps;
            var sourcePropertyValue = record.Metadata.GetType().GetProperty(sourceProperty).GetValue(record.Metadata, null).ToString();
            foreach (var sourceValue in MlEncode.ListElementValues(sourcePropertyValue).ToList())
            {
                if (!string.IsNullOrEmpty(sourceValue.Trim()))
                {
                    tempProps = new FileProperties();

                    /* extract absolute URI and extension */
                    if (sourceProperty != "Format")
                    {
                        var format = MlEncode.Element("format", record.Metadata.Format).FirstOrDefault();
                        if (format != null && !string.IsNullOrEmpty(format.Value))
                        {
                            tempProps.Extension = GetExtension(format.Value);
                        }
                        tempProps.AbsoluteUri = sourceValue.Trim();
                    }
                    else if (sourceValue.StartsWith("application"))
                    {
                        var tmpSplit = sourceValue.Split('\n');
                        if (tmpSplit.Length == 2 &&
                            !string.IsNullOrEmpty(tmpSplit[0].Trim()) &&
                            !string.IsNullOrEmpty(tmpSplit[1].Trim()))
                        {
                            tempProps.Extension = GetExtension(tmpSplit[0].Trim());
                            tempProps.AbsoluteUri = tmpSplit[1].Trim();
                        }
                    }

                    /* extract file name */
                    string fileName = tempProps.AbsoluteUri.Substring(tempProps.AbsoluteUri.LastIndexOf('/'));
                    int questionmarkIndex = fileName.IndexOf('?');
                    if (questionmarkIndex > -1)
                    {
                        fileName = fileName.Substring(0, questionmarkIndex);
                    }
                    tempProps.Name = fileName;

                    /* check if this is an URI */
                    Uri sourceUri;
                    if (Uri.TryCreate(sourceValue, UriKind.Absolute, out sourceUri))
                    {
                        tempProps.Uri = sourceUri;
                        yield return tempProps;
                    }
                }
            }
        }

        public static string TwoTierRequestDownload(
            string baseUrl,
            RecordQueryResult record,
            string fileURI,
            string basePath,
            bool keepFileName,
            string fileExtension,
            string firstTierMethod,
            string secondTierMethod,
            string matchRegex,
            string valueRegex,
            string foundOption)
        {
            if (record == null || record.Metadata == null || string.IsNullOrEmpty(record.Metadata.Format))
            {
                return null;
            }

            try
            {
                basePath = basePath.Trim();
                if (string.IsNullOrEmpty(fileURI) || string.IsNullOrEmpty(basePath))
                {
                    return null;
                }

                CookieContainer cookies = new CookieContainer();
                string returnData = string.Empty;

                /* first tier */
                /* lets get cookies and page */
                var response = firstTierMethod.ToLower() == "get" ? GetResponse(fileURI, cookies) : PostResponse(fileURI, "", cookies);

                /* get key */
                string foundValue = FindInPage(response, matchRegex, valueRegex);

                /* proceed only if we got the key */
                if (!string.IsNullOrEmpty(foundValue))
                {
                    /* second tier */
                    switch (foundOption.ToLower().Trim())
                    {
                        case "key":
                            foundValue = "key=" + foundValue;
                            response = secondTierMethod.ToLower() == "get" ? GetResponse(fileURI, cookies) : PostResponse(fileURI, foundValue, cookies); //(HttpWebResponse)request.GetResponse();
                            break;

                        case "concatenateurl":
                            string completeUrl;
                            string authority = new Uri(baseUrl).GetLeftPart(UriPartial.Authority);
                            if (authority.LastIndexOf('/') == authority.Length - 1 && foundValue.IndexOf('/') == 0)
                            {
                                completeUrl = authority + foundValue.Substring(1);
                            }
                            else
                            {
                                completeUrl = authority + foundValue;
                            }

                            completeUrl = HttpUtility.UrlDecode(completeUrl);
                            completeUrl = HttpUtility.HtmlDecode(completeUrl);

                            response = secondTierMethod.ToLower() == "get" ? GetResponse(completeUrl, cookies) : PostResponse(completeUrl, "", cookies); //(HttpWebResponse)request.GetResponse();
                            break;
                    }

                    string fileName = GetOrCreateFileName(response, keepFileName, record.Header.OAI_Identifier);
                    string filePath = CreateUniqueFileName(basePath, fileName, fileExtension);

                    /* try to download file */
                    var succeeded = DownloadFile(filePath, response);

                    return succeeded ? filePath : null;
                }
            }
            catch (Exception e)
            {
                /* for debugging purpose only */
                string msg = e.Message;
            }
            return null;
        }

        public static string FromSourceTag(
            string baseUrl,
            RecordQueryResult record,
            string firstElement,
            string secondElement,
            string basePath,
            bool keepFileName,
            string fileExtension,
            string firstTierMethod,
            string secondTierMethod,
            string matchRegex,
            string valueRegex,
            string foundOption)
        {
            basePath = basePath.Trim();
            if (string.IsNullOrEmpty(basePath))
            {
                return null;
            }

            try
            {
                string files = string.Empty;
                foreach (var sourceItem in GetAllSources(record, "Source"))
                {
                    string filePath = CreateUniqueFileName(basePath, sourceItem.Name, sourceItem.Extension);

                    if (DownloadFile(filePath, sourceItem.AbsoluteUri))
                    {
                        files += filePath + "][";
                    }
                }
                if (!string.IsNullOrEmpty(files))
                {
                    return files;
                }


                var identifier = MlEncode.Element("identifier", record.Metadata.Identifier).FirstOrDefault();
                if (identifier != null && !string.IsNullOrEmpty(identifier.Value))
                {
                    return TwoTierRequestDownload(
                        baseUrl,
                        record,
                        identifier.Value.Trim(),
                        basePath,
                        keepFileName,
                        fileExtension,
                        "GET",
                        "GET",
                        @"<li><a href=""/viewdoc/download?",
                        @"(?<=\bhref="")[^""]*",
                        "ConcatenateUrl");
                }
            }
            catch (Exception e)
            {
                /* for debugging purpose only */
                string msg = e.Message;
            }
            return null;
        }

        public static void GetFile(string baseURL, RecordQueryResult record)
        {
            if (string.IsNullOrEmpty(baseURL) || record == null)
            {
                return;
            }

            string basePath = Directory.GetCurrentDirectory() + "\\HarvestedFiles\\";
            var fileUri = new Uri(baseURL);

            switch (fileUri.GetLeftPart(UriPartial.Authority))
            {
                case "http://dkum.uni-mb.si":
                    /* save files to dedicated folder */
                    basePath += new Uri(baseURL).Host + "\\";
                    if (Properties.overwriteHarvestedFiles ? true : string.IsNullOrEmpty(record.Header.FilePath) &&
                        FormatList.IsInFormat(record.Metadata.MdFormat, Enums.MetadataFormats.DublinCore) &&
                        !string.IsNullOrEmpty(record.Metadata.Format))
                    {
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        var format = MlEncode.Element("format", record.Metadata.Format).FirstOrDefault();
                        if (format != null && !string.IsNullOrEmpty(format.Value))
                        {
                            string[] formatVal = format.Value.Split('\n');
                            if (formatVal.Length == 2)
                            {
                                string fileExtension = GetExtension(formatVal[0].Trim());
                                string fileUrl = formatVal[1].Trim();
                                record.Header.FilePath = TwoTierRequestDownload(
                                    baseURL, record, fileUrl, basePath, true, fileExtension,
                                    "GET", "POST",
                                    @"<input type=""hidden"" name=""key"" value=""[a-zA-Z0-9]+"" />",
                                    @"(?<=\bvalue="")[^""]*", "key");
                            }
                        }
                    }
                    break;

                case "http://citeseerx.ist.psu.edu":
                    basePath += new Uri(baseURL).Host + "\\";
                    if (Properties.overwriteHarvestedFiles ? true : string.IsNullOrEmpty(record.Header.FilePath) &&
                        FormatList.IsInFormat(record.Metadata.MdFormat, Enums.MetadataFormats.DublinCore) &&
                        !string.IsNullOrEmpty(record.Metadata.Format))
                    {
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        var source = MlEncode.Element("source", record.Metadata.Source).FirstOrDefault();
                        var format = MlEncode.Element("format", record.Metadata.Format).FirstOrDefault();
                        if (source != null && !string.IsNullOrEmpty(source.Value) &&
                            format != null && !string.IsNullOrEmpty(format.Value))
                        {
                            record.Header.FilePath = "";//FromSourceTag(baseURL, record, basePath, true, GetExtension(format.Value.Trim()));
                        }
                    }
                    break;

                default:
                    if (baseURL == Properties.baseURL)
                    {
                        /* save files to dedicated folder */
                        basePath += "localhostApi\\";
                        if (string.IsNullOrEmpty(record.Header.FilePath) &&
                            FormatList.IsInFormat(record.Metadata.MdFormat, Enums.MetadataFormats.DublinCore) &&
                            !string.IsNullOrEmpty(record.Metadata.Format))
                        {
                            if (!Directory.Exists(basePath))
                            {
                                Directory.CreateDirectory(basePath);
                            }

                            /* try DKUM path */
                            var format = MlEncode.Element("format", record.Metadata.Format).FirstOrDefault();
                            if (format != null && !string.IsNullOrEmpty(format.Value))
                            {
                                string[] formatVal = format.Value.Split(' ');
                                if (formatVal.Length == 2)
                                {
                                    string fileExtension = GetExtension(formatVal[0].Trim());
                                    string fileUrl = formatVal[1].Trim();
                                    record.Header.FilePath = TwoTierRequestDownload(
                                        baseURL, record, fileUrl, basePath, true, fileExtension,
                                        "GET", "POST",
                                        @"<input type=""hidden"" name=""key"" value=""[a-zA-Z0-9]+"" />",
                                        @"(?<=\bvalue="")[^""]*", "key");
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public class FileProperties
        {
            public string AbsoluteUri { get; set; }
            public Uri Uri { get; set; }
            public string Name { get; set; }
            public string Extension { get; set; }
        }
    }
}