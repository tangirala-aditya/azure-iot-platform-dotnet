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
            string[] eventOpTypes = { "createDeviceIdentity", "deviceConnected", "deviceDisconnected", "updateTwin", "deleteDeviceIdentity" };
            bool exceptionOccurred = false;
            List<Task> list = new List<Task>();

            var eventGroupList = events.ToList().GroupBy(x => new { Tenant = x.Properties["tenant"], DeviceId = x.SystemProperties["iothub-connection-device-id"] });

            foreach (var eventGroup in eventGroupList)
            {
                try
                {
                    var tenant = eventGroup.Key.Tenant;

                    if (tenant != null)
                    {
                        var deviceId = eventGroup.Key.DeviceId;

                        EventData[] eventDatas = eventGroup.ToArray();
                        eventDatas = eventDatas.Where(x => eventOpTypes.Contains(x.Properties["opType"])).ToArray();
                        if (eventDatas.Count() > 0)
                        {
                            DeviceService deviceService = new DeviceService();
                            list.Add(Task.Run(async () => await deviceService.ProcessDeviceTwin(eventDatas, log, Convert.ToString(tenant), deviceId.ToString())));
                        }
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