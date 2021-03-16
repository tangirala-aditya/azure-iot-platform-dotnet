// <copyright file="JobTriggeredDeployments.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.DeploymentSync.Shared;
using Mmm.Iot.Functions.DeploymentSync.Shared.Helpers;
using Mmm.Iot.Functions.DeploymentSync.Shared.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync
{
    public static class JobTriggeredDeployments
    {
        [FunctionName("JobTriggeredDeployments")]
        public static async Task Run([QueueTrigger("devicesfordeployment", Connection = "AzureStorageConnectionString")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            if (!string.IsNullOrEmpty(myQueueItem))
            {
                DeploymentRequest deploymentRequest = JsonConvert.DeserializeObject<DeploymentRequest>(myQueueItem);

                if (deploymentRequest != null && !string.IsNullOrWhiteSpace(deploymentRequest.TenantId) && !string.IsNullOrWhiteSpace(deploymentRequest.DeviceGroupId) && deploymentRequest.Devices != null && deploymentRequest.Devices.Count > 0)
                {
                    List<DeploymentServiceModel> deployments = new List<DeploymentServiceModel>();
                    try
                    {
                        DeploymentSyncService service = new DeploymentSyncService();

                        deployments.AddRange(await service.GetDeployments(deploymentRequest.TenantId));

                        deployments = deployments.Where(d => !d.Tags.Contains("reserved.inactive") && !d.Tags.Contains("reserved.isDeleted") && !d.Tags.Contains("reserved.reactivated")).ToList();
                    }
                    catch (Exception)
                    {
                        log.LogError($"Error occurrred while fetching deployments");
                        throw;
                    }

                    if (deployments.Count > 0)
                    {
                        var deploymentToProcess = deployments.Where(d => d.DeviceGroupId == deploymentRequest.DeviceGroupId).OrderByDescending(o => o.CreatedDateTime).FirstOrDefault();

                        if (deploymentToProcess != null && deploymentToProcess.Tags.Contains("reserved.latest"))
                        {
                            var packageConfiguration = JsonConvert.DeserializeObject<Configuration>(deploymentToProcess.PackageContent);

                            var result = JSONConverter.DotNotationToDictionary(packageConfiguration.Content.DeviceContent);
                            var jsonResult = JsonConvert.SerializeObject(result);
                            var desiredTwin = JsonConvert.DeserializeObject<Twin>(jsonResult);

                            string targetCondition = $"({string.Join(" or ", deploymentRequest.Devices.Select(v => $"deviceId = '{v}'"))})";

                            var tags = new TwinCollection();
                            tags["configurations"] = new TwinCollection();
                            tags["configurations"]["applied"] = deploymentToProcess.Id;
                            desiredTwin.Tags = tags;

                            var jobId = Guid.NewGuid().ToString();
                            try
                            {
                                await TenantConnectionHelper.GetJobClient(deploymentRequest.TenantId).ScheduleTwinUpdateAsync(jobId, targetCondition, desiredTwin, DateTime.UtcNow, 600);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                        }
                    }
                }
            }
        }
    }
}