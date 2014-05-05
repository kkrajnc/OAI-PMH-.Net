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

namespace FederatedSearch.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Header")]
    public partial class Header
    {
        [Key]
        [Required]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int HeaderId { get; set; }
        public Nullable<System.DateTime> EnterDate { get; set; }
        public string OAI_Identifier { get; set; }
        public string OAI_Set { get; set; }
        public Nullable<System.DateTime> Datestamp { get; set; }
        [Required]
        public bool Deleted { get; set; }
        public string FilePath { get; set; }

        [ForeignKey("OAIDataProviderId")]
        public virtual OAIDataProvider OAIDataProvider { get; set; }
        public Nullable<int> OAIDataProviderId { get; set; }
    }
}
