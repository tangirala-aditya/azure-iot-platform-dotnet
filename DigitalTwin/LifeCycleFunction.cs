// <copyright file="LifeCycleFunction.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Mmm.Iot.Functions.DigitalTwin
{
    public static class LifeCycleFunction
    {
        [FunctionName("LifeCycleFunction")]
        public static async Task Run([EventHubTrigger(eventHubName: "lifecycleevents", Connection = "LifecycleEventHubConnectionString", ConsumerGroup = "%DeviceStreamConsumerGroup%")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData message in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(message.Body.Array);

                    message.SystemProperties.TryGetValue("iothub-connection-device-id", out object deviceId);
                    message.Properties.TryGetValue("opType", out object operationType);

                    if (operationType.ToString().Equals("createDeviceIdentity") || operationType.ToString().Equals("deleteDeviceIdentity"))
                    {
                        string adtInstanceUrl = Environment.GetEnvironmentVariable("DigitalTwinInstanceUrl", EnvironmentVariableTarget.Process);
                        string tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process);
                        string clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
                        string clientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);
                        var credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
                        var client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credentials);

                        string twinId = deviceId.ToString();
                        if (operationType.ToString().Equals("createDeviceIdentity"))
                        {
                            var initData = new BasicDigitalTwin
                            {
                                Id = twinId,
                                Metadata = { ModelId = "dtmi:mmm:device;1" },

                                // Initialize properties
                                Contents =
                                        {
                                            { "Temperature", 0.0 },
                                            { "Humidity", 0.0 },
                                        },
                            };

                            await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(twinId, initData);
                        }
                        else if (operationType.ToString().Equals("deleteDeviceIdentity"))
                        {
                            await client.DeleteDigitalTwinAsync(twinId);
                        }
                    }
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