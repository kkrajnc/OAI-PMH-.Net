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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FederatedSearch.Models
{
    public partial class Set
    {
        [NotMapped]
        public IList<Metadata> AdditionalDescriptions { get; set; }

        public Set MergeSetAndDescription(IList<Metadata> descs)
        {
            this.AdditionalDescriptions = descs;
            return this;
        }
    }
}