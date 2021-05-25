using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ResourceDataCollector.Shared;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;

namespace ResourceDataCollector
{
    public static class DataCollector
    {
        [FunctionName("DataCollector")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            TableStorageOperations cloudTableClient = await TableStorageOperations.GetClientAsync();

            TableQuery<TenantModel> query = new TableQuery<TenantModel>().Where(TableQuery.GenerateFilterConditionForBool(nameof(TenantModel.IsIotHubDeployed), QueryComparisons.Equal, true));
            List<TenantModel> tenants = await cloudTableClient.QueryAsync<TenantModel>("tenant", query);

            List<TenantAccessInformation> tenantsAccessInformation = new List<TenantAccessInformation>();
            if (tenants != null && tenants.Count > 0)
            {
                TenantService tenantService = new TenantService();

                foreach (var tenant in tenants)
                {
                    TenantAccessInformation tenantAccessInformation = new TenantAccessInformation();
                    tenantAccessInformation.TenantId = tenant.TenantId;

                    var docs = await tenantService.GetDocuments(tenant.TenantId);

                    if (docs != null && docs.Count > 0)
                    {
                        tenantAccessInformation.LastAccessDateTime = docs.First().Timestamp;
                        tenantAccessInformation.IsTelemetryFound = true;
                        tenantsAccessInformation.Add(tenantAccessInformation);
                    }
                    else
                    {
                        tenantAccessInformation.IsTelemetryFound = false;
                        tenantsAccessInformation.Add(tenantAccessInformation);
                    }
                }
            }

            return new OkObjectResult(tenantsAccessInformation);
        }
    }
}
