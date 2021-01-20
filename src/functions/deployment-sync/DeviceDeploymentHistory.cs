// <copyright file="DeviceDeploymentHistory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.DeploymentSync.Shared;
using Mmm.Iot.Functions.DeploymentSync.Shared.Helpers;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync
{
    public static class DeviceDeploymentHistory
    {
        [FunctionName("DeviceDeploymentHistory")]
        public static async Task Run([EventHubTrigger(eventHubName: "twin-change", Connection = "TwinChangeEventHubConnectionString", ConsumerGroup = "%DeviceDeploymentHistoryConsumerGroup%")] EventData[] events, ILogger log)
        {
            bool exceptionOccurred = false;
            List<Task> list = new List<Task>();
            foreach (EventData message in events)
            {
                try
                {
                    message.Properties.TryGetValue("tenant", out object tenant);

                    if (tenant != null)
                    {
                        string eventData = Encoding.UTF8.GetString(message.Body.Array);
                        message.SystemProperties.TryGetValue("iothub-connection-device-id", out object deviceId);
                        Twin twin = JsonConvert.DeserializeObject<Twin>(eventData);
                        var newTwin = await TenantConnectionHelper.GetRegistry(Convert.ToString(tenant)).GetTwinAsync(deviceId.ToString());
                        string appliedConfigurationId = string.Empty;
                        var appliedConfigurations = newTwin.Configurations.Where(c => c.Value.Status.Equals(ConfigurationStatus.Applied));
                        if (appliedConfigurations.Count() > 0)
                        {
                            DeploymentSyncService service = new DeploymentSyncService();
                            var deployments = service.GetDeploymentsByIdFromStorage(Convert.ToString(tenant), appliedConfigurations.Select(ac => ac.Key).ToArray());
                            appliedConfigurationId = appliedConfigurations.First().Key;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"Error occurrred : {ex.Message} StackTrace: {ex.StackTrace}  Inner Exception: {(string.IsNullOrEmpty(ex.StackTrace) ? string.Empty : ex.StackTrace)}");
                    exceptionOccurred = true;
                }
            }

            if (exceptionOccurred)
            {
                throw new Exception("Function Failed with exception");
            }
        }
    }
}