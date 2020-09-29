// <copyright file="TenantDataCollection.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TenantDataManagement
{
    public static class TenantDataCollection
    {
        [FunctionName("TenantDataCollection")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            TenantDataCollectionService tenantDataCollectionService = new TenantDataCollectionService();
            await tenantDataCollectionService.GetAzureReourcesData();
            return new OkObjectResult(string.Empty);
        }
    }
}