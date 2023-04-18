using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CRUD.Models
{

    public class CosmosConfiguration
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string Database { get; set; }
        public string Container { get; set; }

        //Role based access
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        //Resource Token
        public string ResourceToken { get; set; }

        public string UserName { get; set; }
        public string PermissionId { get; set; }

        //throughput
        public int ThroughPut { get; set; }

    }

    public class SourceCosmosConfiguration : CosmosConfiguration { }

    public class DestinationCosmosConfiguration : CosmosConfiguration { }

}
