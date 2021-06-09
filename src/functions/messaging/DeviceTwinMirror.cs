// <copyright file="DeviceTwinMirror.cs" company="3M">
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
using Mmm.Iot.Functions.Messaging.Shared;
using Mmm.Iot.Functions.Messaging.Shared.Helpers;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.Messaging
{
    public static class DeviceTwinMirror
    {
        [FunctionName("DeviceTwinMirror")]
        public static async Task Run([EventHubTrigger(eventHubName: "device-twin-mirror", Connection = "DeviceTwinMirrorEventHubConnectionString", ConsumerGroup = "%DeviceStreamConsumerGroup%")] EventData[] events, ILogger log)
        {
            bool exceptionOccurred = false;
            List<Task> list = new List<Task>();

            var eventList = events.ToList().GroupBy(x => new { Tenant = x.Properties["tenant"], DeviceId = x.SystemProperties["iothub-connection-device-id"] });

            foreach (EventData message in events)
            {
                try
                {
                    message.Properties.TryGetValue("tenant", out object tenant);

                    if (tenant != null)
                    {
                        string eventData = Encoding.UTF8.GetString(message.Body.Array);
                        message.SystemProperties.TryGetValue("iothub-connection-device-id", out object deviceId);
                        message.Properties.TryGetValue("opType", out object operationType);

                        DeviceService deviceService = new DeviceService();
                        list.Add(Task.Run(async () => await deviceService.SaveDeviceTwinOperationAsync(eventData, Convert.ToString(tenant), deviceId.ToString(), operationType.ToString())));
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"Error occurrred in for loop: {ex.Message} StackTrace: {ex.StackTrace}  Inner Exception: {(string.IsNullOrEmpty(ex.StackTrace) ? string.Empty : ex.StackTrace)}");
                    exceptionOccurred = true;
                }
            }

            try
            {
                await Task.WhenAll(list.ToArray());
            }
            catch (Exception ex)
            {
                log.LogError($"Error occurrred : {ex.Message} StackTrace: {ex.StackTrace}  Inner Exception: {(string.IsNullOrEmpty(ex.StackTrace) ? string.Empty : ex.StackTrace)}");
                exceptionOccurred = true;
            }

            if (exceptionOccurred)
            {
                throw new Exception("Function Failed with exception");
            }
        }
    }
}