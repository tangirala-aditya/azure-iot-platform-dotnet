// <copyright file="BatchTelemetryProcessor.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.TelemetryProcessor.Helpers;
using Mmm.Iot.Functions.TelemetryProcessor.Shared;

namespace Mmm.Iot.Functions.TelemetryProcessor
{
    public class BatchTelemetryProcessor
    {
        private readonly IConfiguration configuration;
        private readonly IConfigurationRefresher configurationRefresher;

        public BatchTelemetryProcessor(IConfiguration configuration, IConfigurationRefresherProvider refresherProvider)
        {
            this.configuration = configuration;
            this.configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName("BatchTelemetryProcessor")]
        public async Task Run([EventHubTrigger("telemetry", Connection = "TelemetryEventHubConnectionString", ConsumerGroup = "%DeviceStreamConsumerGroup%")] EventData[] events, ILogger log)
        {
            try
            {
                var batchThreshold = Convert.ToInt32(Environment.GetEnvironmentVariable("BatchThreshold", EnvironmentVariableTarget.Process));
                var batchWriteDelay = Convert.ToInt32(Environment.GetEnvironmentVariable("BatchWriteDelay", EnvironmentVariableTarget.Process));
                AppConfigHelper configHelper = new AppConfigHelper(this.configuration, this.configurationRefresher);

                DeviceService deviceService = new DeviceService();
                await deviceService.ProcessTelemetryAsync(
                    events,
                    log,
                    configHelper,
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