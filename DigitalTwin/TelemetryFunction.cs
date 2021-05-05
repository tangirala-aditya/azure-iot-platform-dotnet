// <copyright file="TelemetryFunction.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.DigitalTwin
{
    public static class TelemetryFunction
    {
        [FunctionName("TelemetryFunction")]
        public static async Task Run([EventHubTrigger(eventHubName: "telemetry", Connection = "TelemetryEventHubConnectionString", ConsumerGroup = "%DeviceStreamConsumerGroup%")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData message in events)
            {
                try
                {
                    JObject deviceMessage = JObject.Parse(Encoding.UTF8.GetString(message.Body.Array));

                    // <Find_device_ID_and_temperature>
                    message.SystemProperties.TryGetValue("iothub-connection-device-id", out object deviceId);
                    var temperature = deviceMessage["temperature"].ToString();
                    var humidity = deviceMessage["humidity"].ToString();

                    string adtInstanceUrl = Environment.GetEnvironmentVariable("DigitalTwinInstanceUrl", EnvironmentVariableTarget.Process);
                    string tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process);
                    string clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
                    string clientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);
                    var credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
                    var client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credentials);

                    // <Update_twin_with_device_temperature>
                    var updateTwinData = new JsonPatchDocument();
                    updateTwinData.AppendReplace("/Temperature", double.Parse(temperature));
                    updateTwinData.AppendReplace("/Humidity", double.Parse(humidity));

                    await client.UpdateDigitalTwinAsync(deviceId.ToString(), updateTwinData);
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