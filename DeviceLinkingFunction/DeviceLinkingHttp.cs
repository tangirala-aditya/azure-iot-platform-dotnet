// <copyright file="DeviceLinkingHttp.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.DeviceLinking.Shared;
using Mmm.Iot.Functions.DeviceLinking.Shared.Models;
using Newtonsoft.Json;

namespace Functions
{
    public static class DeviceLinkingHttp
    {
        [FunctionName("DeviceLinkingHttp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var exceptions = new List<Exception>();

            try
            {
                var tenant = "39fea630-4194-4e2f-a216-9131e35da699";
                var deviceLinkingRequest = new DeviceLinkingRequest();
                deviceLinkingRequest.Category = SourceCategory.DeviceGroup;
                deviceLinkingRequest.DeviceGroupId = "ffe5cadf-fbdd-480e-85e8-9c1a69d06981";
                deviceLinkingRequest.ParentDeviceId = "edge-3";
                deviceLinkingRequest.JobId = "9b1f717c-3596-44a1-8628-87131473d99c";

                DeviceService deviceService = new DeviceService();

                await deviceService.ProcessDeviceLinking(deviceLinkingRequest, tenant.ToString());
            }
            catch (Exception e)
            {
                // We need to keep processing the rest of the batch - capture this exception and continue.
                // Also, consider capturing details of the message that failed processing so it can be processed again later.
                exceptions.Add(e);
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.
            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }

            if (exceptions.Count == 1)
            {
                throw exceptions.Single();
            }

            return new OkObjectResult(true);
        }
    }
}