// <copyright file="DeviceLinking.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.DeviceLinking.Shared;
using Mmm.Iot.Functions.DeviceLinking.Shared.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeviceLinking
{
    public static class DeviceLinking
    {
        [FunctionName("DeviceLinking")]
        public static async Task Run([EventHubTrigger("devicelinkingjobhub", Connection = "DeviceLinkingEventHubConnectionString", ConsumerGroup = "%DeviceLinkingConsumerGroup%")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData message in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(message.Body.Array, message.Body.Offset, message.Body.Count);

                    message.Properties.TryGetValue("tenant", out object tenant);

                    var deviceLinkingRequest = JsonConvert.DeserializeObject<DeviceLinkingRequest>(messageBody);

                    DeviceService deviceService = new DeviceService();

                    await deviceService.ProcessDeviceLinking(deviceLinkingRequest, tenant.ToString());
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
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
        }
    }
}