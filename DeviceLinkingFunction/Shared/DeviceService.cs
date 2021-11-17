// <copyright file="DeviceService.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Mmm.Iot.Functions.DeviceLinking.Shared.Exceptions;
using Mmm.Iot.Functions.DeviceLinking.Shared.Helpers;
using Mmm.Iot.Functions.DeviceLinking.Shared.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.DeviceLinking.Shared
{
    public class DeviceService
    {
        private const string QueryPrefix = "SELECT * FROM devices";
        private const string CollectionKey = "pcs";

        private FeedOptions DefaultQueryOptions => new FeedOptions
        {
            EnableCrossPartitionQuery = true,
            EnableScanInQuery = true,
        };

        public async Task ProcessDeviceLinking(DeviceLinkingRequest deviceLinkingRequest, string tenantId)
        {
            if (deviceLinkingRequest != null)
            {
                List<Device> devices = new List<Device>();
                StringBuilder hubQueryBuilder = new StringBuilder();
                switch (deviceLinkingRequest.Category)
                {
                    case SourceCategory.DeviceGroup:
                        var deviceGroupQuery = await this.GetDeviceGroupQuery(tenantId, deviceLinkingRequest.DeviceGroupId);
                        hubQueryBuilder.Append(deviceGroupQuery);
                        break;
                    case SourceCategory.Devices:
                        var deviceIdsQuery = this.GetDeviceIdsQuery(deviceLinkingRequest.DeviceIds);
                        hubQueryBuilder.Append(deviceIdsQuery);
                        break;
                    default:
                        break;
                }

                Device parentDevice = await this.GetDeviceFromHub(tenantId, deviceLinkingRequest.ParentDeviceId);
                var devicesFromQuery = this.GetDevicesInQuery(hubQueryBuilder.ToString(), tenantId);
                if (devicesFromQuery != null && devicesFromQuery.Count > 0)
                {
                    devices.AddRange(devicesFromQuery);
                }

                var operationResult = await this.LinkDevicesToGateway(tenantId, devices, parentDevice);

                // Need to store the operation result for Audit
                await this.UpdateJobStatus(tenantId, deviceLinkingRequest.JobId, deviceLinkingRequest.ParentDeviceId, operationResult);
            }
        }

        public async Task<OperationResult> LinkDevicesToGateway(string tenantId, List<Device> devices, Device parentDevice)
        {
            bool isSucess = true;
            foreach (Device leafDevice in devices)
            {
                if (leafDevice.Capabilities.IotEdge)
                {
                    leafDevice.ParentScopes.Add(parentDevice.Scope);
                }
                else
                {
                    leafDevice.Scope = parentDevice.Scope;
                }

                var result = await TenantConnectionHelper.GetRegistry(tenantId).UpdateDeviceAsync(leafDevice);
                isSucess = isSucess && result != null;
            }

            return new OperationResult() { IsSuccessful = isSucess };
        }

        /*public async Task<OperationResult> LinkDeviceTwinsToGateway(string tenantId, List<Twin> devices, Device parentDevice)
        {
            foreach (Twin leafDevice in devices)
            {
                leafDevice.DeviceScope = parentDevice.Scope;
            }

            var result = await TenantConnectionHelper.GetRegistry(tenantId).UpdateTwins2Async(devices);
            return new OperationResult(result);
        }*/

        private async Task<Device> GetDeviceFromHub(string tenantId, string deviceId)
        {
            return await TenantConnectionHelper.GetRegistry(tenantId).GetDeviceAsync(deviceId);
        }

        private async Task<string> GetDeviceGroupQuery(string tenantId, string deviceGroupId)
        {
            var sql = CosmosOperations.GetDocumentsByProperty("Key", deviceGroupId);

            List<Document> docs = new List<Document>();

            try
            {
                CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
                docs = await storageClient.QueryDocumentsAsync(
                   "pcs-storage",
                   $"pcs-{tenantId}",
                   this.DefaultQueryOptions,
                   sql,
                   0,
                   1);

                if (docs != null && docs.Count() > 0)
                {
                    var deviceGroupDoc = docs.First();

                    var deviceGroupData = deviceGroupDoc.GetPropertyValue<string>("Data");

                    var deviceGroup = JsonConvert.DeserializeObject<DeviceGroup>(deviceGroupData);
                    if (deviceGroup.Conditions != null && deviceGroup.Conditions.Count() > 0)
                    {
                        var targetCondition = QueryConditionTranslator.ToQueryString(JsonConvert.SerializeObject(deviceGroup.Conditions));
                        return targetCondition;
                    }
                }

                return string.Empty;
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No DeviceGroup Document exist in CosmosDb. The collection {$"pcs-{tenantId}"} does not exist.", e);
            }
        }

        private async Task<bool> UpdateJobStatus(string tenantId, string jobId, string parentDeviceId, OperationResult operationResult)
        {
            bool isSuccess = true;
            var sql = CosmosOperations.GetDocumentsByProperty("Key", jobId);

            List<Document> docs = new List<Document>();

            try
            {
                CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
                docs = await storageClient.QueryDocumentsAsync(
                   "pcs-storage",
                   $"pcs-{tenantId}",
                   this.DefaultQueryOptions,
                   sql,
                   0,
                   1);

                if (docs != null && docs.Count() > 0)
                {
                    var jobDoc = docs.First();

                    var jobData = jobDoc.GetPropertyValue<string>("Data");

                    string eTag = jobDoc.GetPropertyValue<string>("ETag");

                    var job = JsonConvert.DeserializeObject<DeviceLinkingJob>(jobData);

                    // Update the Operation Result to Job
                    job.JobStatus = "Completed";
                    job.OperationResult = operationResult;

                    string dataToUpdate = JsonConvert.SerializeObject(job);

                    await storageClient.SaveDocumentAsync($"devicelinkingjobs-{parentDeviceId}", jobId, dataToUpdate, this.GenerateCollectionLink(tenantId), null, eTag);
                }
                else
                {
                    isSuccess = false;
                }
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No Job records exist in CosmosDb. The pcs-storage collection {$"pcs-{tenantId}"} does not exist.", e);
            }

            return isSuccess;
        }

        private string GetDeviceIdsQuery(IEnumerable<string> deviceIds)
        {
            string deviceIdCondition = string.Empty;
            if (deviceIds != null && deviceIds.Any())
            {
                deviceIdCondition = $"({string.Join(" or ", deviceIds.Select(v => $"deviceId = '{v}'"))})";
            }

            return deviceIdCondition;
        }

        private List<Device> GetDevicesInQuery(string hubQuery, string tenantId)
        {
            var query = string.IsNullOrWhiteSpace(hubQuery) ? QueryPrefix : $"{QueryPrefix} where {hubQuery}";
            var queryResponse = TenantConnectionHelper.GetRegistry(tenantId).CreateQuery(query);
            var devices = new List<Device>();

            try
            {
                while (queryResponse.HasMoreResults)
                {
                    // TODO: Add pagination with queryOptions
                    var resultSet = queryResponse.GetNextAsJsonAsync();
                    foreach (var result in resultSet.Result)
                    {
                        var device = JsonConvert.DeserializeObject<Device>(result);
                        device.ETag = JObject.Parse(result)["deviceEtag"].ToString();
                        devices.Add(device);
                    }
                }
            }
            catch (Exception)
            {
                // this.logger.LogError(ex, "Error getting status of devices in query {query}", query);
            }

            return devices;
        }

        private string GenerateCollectionLink(string tenant)
        {
            var cosmosDbcoll = $"{CollectionKey}-{tenant}";
            var cosmosDb = Environment.GetEnvironmentVariable("DeviceLinkingDatabaseId", EnvironmentVariableTarget.Process);
            return $"/dbs/{cosmosDb}/colls/{cosmosDbcoll}";
        }
    }
}