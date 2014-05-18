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
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FederatedSearch.Models
{
    public partial class Header
    {
        [NotMapped]
        public bool IsDatestampDateTime { get; set; }

        public static void AddRecHeaderToDatabase(
            OaiPmhContext context, 
            Header header, 
            OAIDataProvider dataProvider, 
            bool createNewIdentifier, 
            string identifierBase)
        {
            header.OAIDataProviderId = dataProvider.OAIDataProviderId;
            context.Header.Add(header);
            context.SaveChanges();

            if (createNewIdentifier)
            {
                if(identifierBase.ElementAt(identifierBase.Length -1) != ':')
                {
                    identifierBase += ':';
                }

                header.OAI_Identifier = identifierBase + header.HeaderId;
                context.Entry(header).State = System.Data.EntityState.Modified;
            }
        }
    }
}