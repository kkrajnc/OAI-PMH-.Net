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

using FederatedSearch.API.Internal;
using FederatedSearch.API.MdFormats;
using FederatedSearch.Models;
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
        public static string GetOrCreateFileName(HttpWebResponse response, FileProperties fileProperties)
        {
            if (response == null)
            {
                return string.Empty;
            }

            string fileName = null;
            if (response.Headers.AllKeys.Contains("Content-Disposition"))
            {
                string contentDisposition = response.Headers.GetValues("Content-Disposition").FirstOrDefault();
                fileName = string.IsNullOrEmpty(contentDisposition) ?
                            null : new ContentDisposition(contentDisposition).FileName;
            }
            
            if (string.IsNullOrEmpty(fileName) && response.ResponseUri != null)
            {
                fileName = Path.GetFileName(response.ResponseUri.LocalPath);
            }

            if (string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(fileProperties.Name))
            {
                fileName = fileProperties.Name + fileProperties.Extension;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = fileProperties.AlternateName + fileProperties.Extension;
            }

            if (!fileName.Contains('.'))
            {
                fileName += fileProperties.Extension;
            }

            return fileName;
        }

        public static string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            return new Regex("[^a-zA-Z0-9_.-]+", RegexOptions.Compiled).Replace(fileName, "");
        }

        public static string CreateUniqueFileName(string basePath, string fileName)
        {
            string filePath = string.Empty;
            /* create file path */
            filePath = basePath +
                       (string.IsNullOrEmpty(fileName) ? CleanFileName(Helper.CreateGuid()) : CleanFileName(fileName));

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

            if (Properties.limitHarvestedFileTypes ?
                !Properties.allowedMimeTypesList.Contains(GetResponseContentType(response)) : false)
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
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.ContentType = "application/x-www-form-urlencoded";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:27.0) Gecko/20100101 Firefox/27.0";
                request.Referer = uri;
                request.KeepAlive = true;
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

                /* write content to the stream */
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

        public static IEnumerable<string> FindInPage(HttpWebResponse response, string lineRegex, string valueRegex)
        {
            if (response == null || string.IsNullOrEmpty(lineRegex) || string.IsNullOrEmpty(valueRegex))
            {
                yield break;
            }

            bool skipLineCheck = string.IsNullOrEmpty(lineRegex);
            string line = string.Empty;
            using (var responseStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                while ((line = responseStream.ReadLine()) != null)
                {
                    if (skipLineCheck || Regex.IsMatch(line, lineRegex, RegexOptions.Compiled))
                    {
                        yield return new Regex(valueRegex, RegexOptions.Compiled).Match(line).Value;
                    }
                }
            }
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
                var value = sourceValue; /* new variable because we cannot assign to a foreach variable */
                if (!string.IsNullOrEmpty(value.Trim()))
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
                        else
                        {
                            tempProps.Extension = string.Empty;
                        }
                        tempProps.AbsoluteUri = value.Trim();
                    }
                    else if (value.StartsWith("application"))
                    {
                        var tmpSplit = value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tmpSplit.Length == 2)
                        {
                            tempProps.Extension = GetExtension(tmpSplit[0].Trim());
                            tempProps.AbsoluteUri = tmpSplit[1].Trim();
                            value = tmpSplit[1].Trim();
                        }
                    }

                    /* extract file name */
                    string fileName = tempProps.AbsoluteUri.Substring(tempProps.AbsoluteUri.LastIndexOf('/') + 1);
                    int questionmarkIndex = fileName.IndexOf('?');
                    if (questionmarkIndex > -1)
                    {
                        fileName = string.Empty;
                    }
                    tempProps.Name = fileName;

                    tempProps.AlternateName = record.Header.OAI_Identifier;

                    /* check if this is an URI */
                    Uri sourceUri;
                    if (Uri.TryCreate(value, UriKind.Absolute, out sourceUri) &&
                        (sourceUri.Scheme == Uri.UriSchemeHttp ||
                         sourceUri.Scheme == Uri.UriSchemeHttps ||
                         sourceUri.Scheme == Uri.UriSchemeFtp ||
                         sourceUri.Scheme == Uri.UriSchemeFile))
                    {
                        tempProps.Uri = sourceUri;
                        yield return tempProps;
                    }
                }
            }
        }

        public static string PageOnly(FileProperties fileProperties, string basePath)
        {
            if (fileProperties == null || string.IsNullOrEmpty(basePath))
            {
                return null;
            }

            try
            {
                PageFileHarvestProperties properties;
                if (Properties.pageFileHarvestProperties.TryGetValue(fileProperties.Uri.GetLeftPart(UriPartial.Authority), out properties))
                {
                    basePath = basePath.Trim();

                    CookieContainer cookies = new CookieContainer();
                    string returnData = string.Empty;

                    /* first tier */
                    /* lets get page and cookies */
                    var response = properties.FirstHttpMethod.ToLower() == "get" ?
                        GetResponse(fileProperties.AbsoluteUri, cookies) : PostResponse(fileProperties.AbsoluteUri, "", cookies);

                    if (response != null)
                    {
                        /* update because of possible redirection */
                        fileProperties.AbsoluteUri = response.ResponseUri.ToString();
                        fileProperties.Uri = response.ResponseUri;

                        string filePaths = string.Empty;

                        foreach (var foundValue in FindInPage(response, properties.LineRegex, properties.ValueRegex))
                        {
                            /* second tier */
                            switch (properties.SecondTierValueOption.ToLower().Trim())
                            {
                                case "key":
                                    response = properties.SecondHttpMethod.ToLower() == "get" ?
                                        GetResponse(fileProperties.AbsoluteUri, cookies) :
                                        PostResponse(fileProperties.AbsoluteUri, "key=" + foundValue, cookies);
                                    break;

                                case "concatenateurl":
                                    string completeUrl;
                                    string authority = fileProperties.Uri.GetLeftPart(UriPartial.Authority);
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

                                    response = properties.SecondHttpMethod.ToLower() == "get" ?
                                        GetResponse(completeUrl, cookies) : PostResponse(completeUrl, "", cookies);
                                    break;
                            }

                            string fileName = GetOrCreateFileName(response, fileProperties);
                            string filePath = CreateUniqueFileName(basePath, fileName);

                            /* try to download file */
                            if(DownloadFile(filePath, response))
                            {
                                filePaths += filePath + "][";
                            }
                        }

                        if (!string.IsNullOrEmpty(filePaths))
                        {
                            return filePaths;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                /* for debugging purpose only */
                string msg = e.Message;
            }
            return null;
        }

        public static string FromPageOnly(
            RecordQueryResult record,
            string basePath,
            string source)
        {
            try
            {
                if (record == null || string.IsNullOrEmpty(basePath))
                {
                    return null;
                }

                string files = string.Empty;
                foreach (var sourceItem in GetAllSources(record, source))
                {
                    string filePath = PageOnly(sourceItem, basePath);

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        files += filePath;
                    }
                }

                if (!string.IsNullOrEmpty(files))
                {
                    return files.Substring(0, files.Length - 2);
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
            RecordQueryResult record,
            OAIDataProvider dataProvider,
            string basePath)
        {
            try
            {
                if (record == null || dataProvider == null || string.IsNullOrEmpty(basePath))
                {
                    return null;
                }

                string files = string.Empty;
                foreach (var sourceItem in GetAllSources(record, dataProvider.FirstSource))
                {
                    var fileName = string.IsNullOrEmpty(sourceItem.Name) ? sourceItem.AlternateName : sourceItem.Name;
                    string filePath = CreateUniqueFileName(basePath, fileName + sourceItem.Extension);

                    if (DownloadFile(filePath, sourceItem.AbsoluteUri))
                    {
                        files += filePath + "][";
                    }
                }
                if (!string.IsNullOrEmpty(files))
                {
                    return files.Substring(0, files.Length - 2);
                }

                return FromPageOnly(record, basePath, 
                    string.IsNullOrEmpty(dataProvider.SecondSource) ? dataProvider.FirstSource : dataProvider.SecondSource);
            }
            catch (Exception e)
            {
                /* for debugging purpose only */
                string msg = e.Message;
            }
            return null;
        }

        public static void GetFile(OAIDataProvider dataProvider, RecordQueryResult record)
        {
            if (dataProvider == null || record == null)
            {
                return;
            }

            string basePath = Directory.GetCurrentDirectory() + "\\HarvestedFiles\\";
            basePath += new Uri(dataProvider.BaseURL).Host + "\\";
            if (Properties.overwriteHarvestedFiles ? true : string.IsNullOrEmpty(record.Header.FilePath))
            {
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                string filePath = null;
                switch (dataProvider.Function)
                {
                    case "FromPageOnly":
                        filePath = FromPageOnly(record, basePath, dataProvider.FirstSource);
                        break;
                    case "FromSourceTag":
                        filePath = FromSourceTag(record, dataProvider, basePath);
                        break;
                    default:
                        break;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    record.Header.FilePath = filePath;
                }
            }
        }

        public class FileProperties
        {
            public string AbsoluteUri { get; set; }
            public Uri Uri { get; set; }
            public string Name { get; set; }
            public string Extension { get; set; }
            public string AlternateName { get; set; }
        }
    }
}