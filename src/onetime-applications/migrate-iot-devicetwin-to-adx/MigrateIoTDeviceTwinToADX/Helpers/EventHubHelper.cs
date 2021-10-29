// <copyright file="EventHubHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Mmm.Iot.MigrateIoTDeviceTwinToADX.Helpers
{
    public class EventHubHelper
    {
        private readonly string connectionString;

        public EventHubHelper(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public EventHubProducerClient GetClient(string eventHubName)
        {
            return new EventHubProducerClient(this.connectionString, eventHubName);
        }

        public async Task SendMessageToEventHub(string eventHubName, List<EventData> events)
        {
            // Create a producer client that you can use to send events to an event hub
            await using (var producerClient = this.GetClient(eventHubName))
            {
                while (events.Count > 0)
                {
                    int batchCount = 0;

                    // Create a batch of events
                    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                    // Add events to the batch. An event is a represented by a collection of bytes and metadata.
                    foreach (var eventData in events)
                    {
                        if (eventBatch.TryAdd(eventData))
                        {
                            batchCount++;
                        }
                        else
                        {
                            // Use the producer client to send the batch of events to the event hub
                            await producerClient.SendAsync(eventBatch);
                            events.RemoveRange(0, batchCount);
                            batchCount = 0;
                            break;
                        }
                    }

                    if (batchCount > 0)
                    {
                        await producerClient.SendAsync(eventBatch);
                        events.RemoveRange(0, batchCount);
                        batchCount = 0;
                    }
                }
            }
        }
    }
}