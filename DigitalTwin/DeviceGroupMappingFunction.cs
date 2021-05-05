// <copyright file="DeviceGroupMappingFunction.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.DigitalTwin.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DigitalTwin
{
    public static class DeviceGroupMappingFunction
    {
        [FunctionName("DeviceGroupMapping")]
        public static async Task Run([QueueTrigger("devicegroupmapping", Connection = "AzureQueueConnectionString")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
            if (!string.IsNullOrEmpty(myQueueItem))
            {
                DeviceGroupMappingRequest deviceGroupMapping = JsonConvert.DeserializeObject<DeviceGroupMappingRequest>(myQueueItem);

                if (deviceGroupMapping != null && !string.IsNullOrWhiteSpace(deviceGroupMapping.TenantId) && !string.IsNullOrWhiteSpace(deviceGroupMapping.DeviceGroupId) && deviceGroupMapping != null && deviceGroupMapping.Devices.Count > 0)
                {
                    string adtInstanceUrl = Environment.GetEnvironmentVariable("DigitalTwinInstanceUrl", EnvironmentVariableTarget.Process);
                    string tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process);
                    string clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
                    string clientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);
                    var credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
                    var client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credentials);

                    try
                    {
                        var result = await client.GetDigitalTwinAsync<BasicDigitalTwin>(deviceGroupMapping.DeviceGroupId);

                        if (result == null || (result != null && result.Value == null))
                        {
                            var initData = new BasicDigitalTwin
                            {
                                Id = deviceGroupMapping.DeviceGroupId,
                                Metadata = { ModelId = "dtmi:mmm:devicegroup;1" },
                            };

                            await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(deviceGroupMapping.DeviceGroupId, initData);
                        }
                    }
                    catch (Azure.RequestFailedException ex)
                    {
                        if (ex.ErrorCode == "DigitalTwinNotFound")
                        {
                            var initData = new BasicDigitalTwin
                            {
                                Id = deviceGroupMapping.DeviceGroupId,
                                Metadata = { ModelId = "dtmi:mmm:devicegroup;1" },
                            };

                            await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(deviceGroupMapping.DeviceGroupId, initData);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                    foreach (var device in deviceGroupMapping.Devices)
                    {
                        string targetDevice = device.ToString();

                        var relationship = new BasicRelationship
                        {
                            TargetId = targetDevice,
                            Name = "contains",
                        };

                        try
                        {
                            string relId = $"{deviceGroupMapping.DeviceGroupId}-Contains->{targetDevice}";
                            await client.CreateOrReplaceRelationshipAsync<BasicRelationship>(deviceGroupMapping.DeviceGroupId, relId, relationship);
                            Console.WriteLine($"Created Contains relationship successfully. Relationship ID is {relId}.");
                        }
                        catch (RequestFailedException rex)
                        {
                            Console.WriteLine($"Create relationship error: {rex.Status}:{rex.Message}");
                        }
                    }
                }
            }
        }
    }
}