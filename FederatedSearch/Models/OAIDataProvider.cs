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

namespace FederatedSearch.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Threading;
    using System.Threading.Tasks;

    [Table("OAIDataProvider")]
    public partial class OAIDataProvider
    {
        public OAIDataProvider()
        {
            this.Headers = new HashSet<Header>();
        }

        [Key]
        [Required]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int OAIDataProviderId { get; set; }
        public string BaseURL { get; set; }
        public string Granularity { get; set; }
        public Nullable<System.DateTime> LastHarvesting { get; set; }
        public string RepositoryName { get; set; }
        public string ProtocolVersion { get; set; }
        public string AdminEmail { get; set; }
        public Nullable<System.DateTime> EarliestDatestamp { get; set; }
        public string DeletedRecord { get; set; }
        public string Compression { get; set; }

        /* Properties for file harvesting */
        public string Function { get; set; }
        public string FirstSource { get; set; }
        public string SecondSource { get; set; }
    
        public virtual ICollection<Header> Headers { get; set; }
    }

    public class DataProviderProperties
    {
        public int OAIDataProviderId { get; set; }
        public string BaseURL { get; set; }
        public string RepositoryName { get; set; }
        public string From { get; set; }
        public string Until { get; set; }
        public bool Exclude { get; set; }
        public bool FullHarvestDelete { get; set; }
        public bool HarvestDeleteFiles { get; set; }
        public string MetadataPrefix { get; set; }
        public DataProviderHarvestStats Stats { get; set; }
    }

    public class DataProviderHarvestStats
    {
        public int OAIDataProviderId { get; set; }
        public int RatioDone { get; set; }
        public int RatioAll { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class DataProviderIntern
    {
        public OAIDataProvider DataProvider { get; set; }
        public DataProviderProperties HarvestOptions { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }
}
