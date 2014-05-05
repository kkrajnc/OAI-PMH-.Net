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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace FederatedSearch.API.Common
{
    public class Helper
    {
        public static string CreateGuid()
        {
            Guid guidValue = Guid.NewGuid();
            MD5 md5 = MD5.Create();
            return new Guid(md5.ComputeHash(guidValue.ToByteArray())).ToString();
        }

        /* Convert from UTC string to DateTime */
        private static string[] dateFormats = { Enums.Granularity.Date, Enums.Granularity.DateTime };
        public static bool ConvertUTCToDateTime(string utcString, out DateTime dateTime)
        {
            return DateTime.TryParseExact(utcString, dateFormats, CultureInfo.CurrentCulture,
                DateTimeStyles.AssumeUniversal, out dateTime);
        }

        public static bool IsDateTimeFormat(string utcString)
        {
            return utcString.Length == 20;
        }

        public static List<string> GetAllSets(string set)
        {
            if (String.IsNullOrEmpty(set))
            {
                return null;
            }

            string[] setSplit = set.Split(':');
            List<string> sets = new List<string>();

            for (int i = 0; i < setSplit.Length; i++)
            {
                if (i == 0)
                {
                    sets.Add(setSplit[i]);
                }
                else
                {
                    sets.Add(sets[i - 1] + ":" + setSplit[i]);
                }
            }
            return sets;
        }

        public static IEnumerable<List<T>> SplitList<T>(List<T> completeList, int splitListSize = 30)
        {
            for (int i = 0; i < completeList.Count; i += splitListSize)
            {
                yield return completeList.GetRange(i, Math.Min(splitListSize, completeList.Count - i));
            }
        }

        public static T TryToDeserializeJson<T>(string value)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}