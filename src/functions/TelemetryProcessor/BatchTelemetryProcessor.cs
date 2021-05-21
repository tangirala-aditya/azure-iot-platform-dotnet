// <copyright file="BatchTelemetryProcessor.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.TelemetryProcessor.Helpers;
using Mmm.Iot.Functions.TelemetryProcessor.Shared;

namespace Mmm.Iot.Functions.TelemetryProcessor
{
    public static class BatchTelemetryProcessor
    {
        [FunctionName("BatchTelemetryProcessor")]
        public static async Task Run([EventHubTrigger("telemetry", Connection = "TelemetryEventHubConnString")] EventData[] events, ILogger log)
        {
            try
            {
                var batchThreshold = Convert.ToInt32(Environment.GetEnvironmentVariable("BatchThreshold", EnvironmentVariableTarget.Process));
                var batchWriteDelay = Convert.ToInt32(Environment.GetEnvironmentVariable("BatchWriteDelay", EnvironmentVariableTarget.Process));
                EventHubHelper eventHubHelper = new EventHubHelper(Environment.GetEnvironmentVariable("EventHubConnectionString", EnvironmentVariableTarget.Process));

                DeviceService deviceService = new DeviceService();
                await deviceService.ProcessTelemetryAsync(
                    events,
                    log,
                    eventHubHelper,
                    batchThreshold: batchThreshold == 0 ? 12 : batchThreshold,  // default to 12 if the value is 0. 0 causes an exception
                    batchWriteDelay: batchWriteDelay);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Unable to process telemetry message. Error Message: {ex.Message}");
                throw ex;
            }
        }
    }
}