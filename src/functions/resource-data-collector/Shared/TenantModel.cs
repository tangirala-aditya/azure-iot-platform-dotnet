using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Cosmos.Table;

namespace ResourceDataCollector.Shared
{
    public class TenantModel : TableEntity
    {
        public TenantModel()
        {
        }

        public string TenantId { get; set; }

        public string IotHubName { get; set; }

        public string SAJobName { get; set; }

        public bool IsIotHubDeployed { get; set; }

        public string TenantName { get; set; }
    }
}
