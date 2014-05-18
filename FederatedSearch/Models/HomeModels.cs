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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Web;

namespace FederatedSearch.Models
{
    public class OAIModel
    {
        public string Verb { get; set; }
        public string From { get; set; }
        public string Until { get; set; }
        public string MetadataPrefix { get; set; }
        public string Set { get; set; }
        public string ResumptionToken { get; set; }
        public string Identifier { get; set; }
        public string UrlQuery { get; set; }
        public string DkumResult { get; set; }
        public string LocalResult { get; set; }
        public string Ratio { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Verb);
            sb.Append(From);
            sb.Append(Until);
            sb.Append(MetadataPrefix);
            sb.Append(Set);
            sb.Append(ResumptionToken);
            sb.Append(Identifier);
            sb.Append(UrlQuery);
            sb.Append(DkumResult);
            sb.Append(LocalResult);
            sb.Append(Ratio);

            return sb.ToString();
        }
    }
}