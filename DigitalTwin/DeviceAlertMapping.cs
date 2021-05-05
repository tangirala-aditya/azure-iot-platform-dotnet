// <copyright file="DeviceAlertMapping.cs" company="3M">
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
using Mmm.Iot.Functions.DigitalTwin.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.DigitalTwin
{
    public static class DeviceAlertMapping
    {
        [FunctionName("DeviceAlertMapping")]
        public static async Task Run(
            [EventHubTrigger("actions-eventhub", Connection = "ActionsEventHubConnectionString", ConsumerGroup = "%ActionsEventHubConsumerGroup%")] EventData[] events,
            ILogger log,
            ExecutionContext context)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    var messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    var alertMessageSplit = messageBody.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string alertMessage in alertMessageSplit)
                    {
                        try
                        {
                            var actionEvent = JsonConvert.DeserializeObject<ActionEvent>(alertMessage);
                            JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(alertMessage);

                            string adtInstanceUrl = Environment.GetEnvironmentVariable("DigitalTwinInstanceUrl", EnvironmentVariableTarget.Process);
                            string tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process);
                            string clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
                            string clientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);
                            var credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
                            var client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credentials);

                            try
                            {
                                var result = await client.GetDigitalTwinAsync<BasicDigitalTwin>(actionEvent.DeviceId);

                                if (result == null || (result != null && result.Value == null))
                                {
                                    var initData = new BasicDigitalTwin
                                    {
                                        Id = actionEvent.DeviceId,
                                        Metadata = { ModelId = "dtmi:mmm:device;1" },
                                    };

                                    await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(actionEvent.DeviceId, initData);
                                }
                            }
                            catch (Azure.RequestFailedException ex)
                            {
                                if (ex.ErrorCode == "DigitalTwinNotFound")
                                {
                                    var initData = new BasicDigitalTwin
                                    {
                                        Id = actionEvent.DeviceId,
                                        Metadata = { ModelId = "dtmi:mmm:device;1" },
                                    };

                                    await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(actionEvent.DeviceId, initData);
                                }
                            }
                            catch (Exception)
                            {
                                throw;
                            }

                            string target = Guid.NewGuid().ToString();
                            try
                            {
                                var initData = new BasicDigitalTwin
                                {
                                    Id = target,
                                    Metadata = { ModelId = "dtmi:mmm:alert;1" },
                                    Contents =
                                      {
                                           { "RuleId", actionEvent.RuleId },
                                           { "RuleDescription", actionEvent.RuleDescription },
                                           { "RuleSeverity", actionEvent.RuleSeverity },
                                           { "CreatedOn", DateTime.UtcNow.ToString() },
                                      },
                                };

                                await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(target, initData);
                            }
                            catch (Azure.RequestFailedException)
                            {
                            }

                            var relationship = new BasicRelationship
                            {
                                TargetId = target,
                                Name = "contains",
                            };

                            try
                            {
                                string relId = $"{actionEvent.DeviceId}-Contains->{target}";
                                await client.CreateOrReplaceRelationshipAsync<BasicRelationship>(actionEvent.DeviceId, relId, relationship);
                                Console.WriteLine($"Created Contains relationship successfully. Relationship ID is {relId}.");
                            }
                            catch (RequestFailedException rex)
                            {
                                Console.WriteLine($"Create relationship error: {rex.Status}:{rex.Message}");
                            }
                        }
                        catch (Exception e)
                        {
                            // We need to keep processing the rest of the batch - capture this exception and continue.
                            exceptions.Add(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    exceptions.Add(new Exception("Unable to process one of the nested alert events.", e));
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.
            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }
            else if (exceptions.Count == 1)
            {
                throw exceptions.Single();
            }
        }
    }
}