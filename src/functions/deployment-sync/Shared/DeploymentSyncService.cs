// <copyright file="DeploymentSyncService.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Mmm.Iot.Functions.DeploymentSync.Shared.Exceptions;
using Mmm.Iot.Functions.DeploymentSync.Shared.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class DeploymentSyncService
    {
        private const string RmCreatedLabel = "RMDeployment";
        private const string DeviceIdKey = "deviceId";
        private const string FailedQueryName = "error";
        private const string SuccessQueryName = "current";
        private const int MaximumGetList = 1000;
        private const string QueryPrefix = "SELECT * FROM devices";
        private const string ModuleQueryPrefix = "SELECT * FROM devices.modules";
        private const string DevicesConnectedQuery = "connectionState = 'Connected'";
        private const string DeploymentsCollection = "deployments";
        private const string DeploymentDevicePropertiesCollection = "deploymentdevices-{0}";
        private const string CollectionKey = "pcs";

        public async Task<List<DeploymentServiceModel>> GetDeploymentsToSync(string tenantId, IEnumerable<Configuration> configurations)
        {
            List<DeploymentServiceModel> deploymentsToProcess = new List<DeploymentServiceModel>();
            try
            {
                List<DeploymentServiceModel> deploymentsFromHub =
                 configurations.Where(this.CheckIfDeploymentWasMadeByRM)
                            .Select(config => new DeploymentServiceModel(config))
                            .OrderByDescending(conf => conf.CreatedDateTimeUtc)
                            .ToList();

                var groupedDeployments = deploymentsFromHub.GroupBy(s => s.DeviceGroupId).Select(d => new
                {
                    DeivceGroupId = d.Key,
                    Deployments = d.OrderByDescending(x => x.Priority).
                    ThenByDescending(x => x.CreatedDateTimeUtc),
                });

                foreach (var item in groupedDeployments)
                {
                    if (item.Deployments?.Count() > 0)
                    {
                        item.Deployments.First().Tags = new List<string> { "reserved.latest" };
                    }
                }

                List<string> deploymentIds = new List<string>();
                deploymentIds.AddRange(deploymentsFromHub.Select(s => s.Id));

                var deploymentsFromStorage = await this.GetDeploymentsFromStorage(tenantId, deploymentIds);

                foreach (var deploymentFromHub in deploymentsFromHub)
                {
                    var deploymentFromStorage = deploymentsFromStorage.FirstOrDefault(d => d.Id == deploymentFromHub.Id);
                    if (deploymentFromStorage != null)
                    {
                        if (deploymentFromStorage.IsLatest != deploymentFromHub.IsLatest)
                        {
                            deploymentFromStorage.Tags = deploymentFromHub.Tags;

                            deploymentsToProcess.Add(deploymentFromStorage);
                        }
                    }
                    else
                    {
                        deploymentsToProcess.Add(deploymentFromHub);
                    }
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }

            return deploymentsToProcess;
        }

        public async Task<DeploymentServiceModel> UpdateAndSaveDeployment(string tenantId, DeploymentServiceModel deployment, Configuration configuration)
        {
            var devicesStatuses = this.GetDeviceStatuses(tenantId, configuration);
            if (devicesStatuses != null && devicesStatuses.Keys.Count > 0)
            {
                deployment.DeploymentMetrics.DeviceStatuses = devicesStatuses;
                deployment.DeploymentMetrics.DeviceMetrics = this.CalculateDeviceMetrics(devicesStatuses);
            }

            await this.SaveDeployment(deployment, tenantId);

            if (devicesStatuses != null && devicesStatuses.Keys.Count > 0)
            {
                var deviceTwins = await this.GetDeviceProperties(tenantId, devicesStatuses.Keys);
                if (deviceTwins != null && deviceTwins.Count > 0)
                {
                    await this.SaveDeviceProperties(tenantId, deployment.Id, deviceTwins);
                }
            }

            return deployment;
        }

        public async Task<TwinServiceListModel> GetModuleTwinsByQueryAsync(
            string tenantId,
            string query,
            string continuationToken)
        {
            var twins = await this.GetTwinByQueryAsync(
                tenantId,
                ModuleQueryPrefix,
                query,
                continuationToken,
                MaximumGetList);
            var result = twins.Result.Select(twin => new TwinServiceModel(twin)).ToList();

            return new TwinServiceListModel(result, twins.ContinuationToken);
        }

        private bool CheckIfDeploymentWasMadeByRM(Configuration conf)
        {
            return conf.Labels != null &&
                   conf.Labels.ContainsKey(RmCreatedLabel) &&
                   bool.TryParse(conf.Labels[RmCreatedLabel], out var res) && res;
        }

        private async Task<List<DeploymentServiceModel>> GetDeploymentsFromStorage(string tenantId, IEnumerable<string> deploymentIds)
        {
            var sql = QueryBuilder.GetDocumentsSql(
                "deployments",
                null,
                null,
                null,
                "_ts",
                null,
                "_ts",
                "asc",
                "_ts",
                0,
                100,
                deploymentIds.ToArray(),
                "Key");

            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                EnableScanInQuery = true,
            };

            List<Document> docs = new List<Document>();

            try
            {
                CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
                docs = await storageClient.QueryDocumentsAsync(
                   "pcs-storage",
                   $"pcs-{tenantId}",
                   queryOptions,
                   sql,
                   0,
                   100);

                var result = docs.Select(doc => new ValueServiceModel(doc));
                var deploymentFromStorage = result.Select(res => this.CreateDeploymentServiceModel(res)).ToList();

                return deploymentFromStorage;
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No deployments exist in CosmosDb. The telemetry collection {$"pcs-{tenantId}"} does not exist.", e);
            }
        }

        private async Task SaveDeployment(DeploymentServiceModel deployment, string tenantId)
        {
            CosmosOperations storageClient = await CosmosOperations.GetClientAsync();

            var value = JsonConvert.SerializeObject(
                                        deployment,
                                        Formatting.Indented,
                                        new JsonSerializerSettings
                                        {
                                            NullValueHandling = NullValueHandling.Ignore,
                                        });

            await storageClient.SaveDocumentAsync(DeploymentsCollection, deployment.Id, new ValueServiceModel() { Data = value, ETag = deployment.ETag }, this.GenerateCollectionLink(tenantId));
        }

        private async Task SaveDeviceProperties(string tenantId, string deploymentId, List<TwinServiceModel> deviceTwins)
        {
            if (deviceTwins != null)
            {
                CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
                foreach (var deviceTwin in deviceTwins)
                {
                    var value = JsonConvert.SerializeObject(
                        deviceTwin,
                        Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                        });

                    try
                    {
                        await storageClient.SaveDocumentAsync(string.Format(DeploymentDevicePropertiesCollection, deploymentId), Guid.NewGuid().ToString(), new ValueServiceModel() { Data = value }, this.GenerateCollectionLink(tenantId));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private string GenerateCollectionLink(string tenant)
        {
            var cosmosDbcoll = $"{CollectionKey}-{tenant}";
            var cosmosDb = Environment.GetEnvironmentVariable("DeploymentsDatabaseId", EnvironmentVariableTarget.Process);
            return $"/dbs/{cosmosDb}/colls/{cosmosDbcoll}";
        }

        private DeploymentServiceModel CreateDeploymentServiceModel(ValueServiceModel response)
        {
            var output = JsonConvert.DeserializeObject<DeploymentServiceModel>(response.Data);
            output.Id = response.Key;
            output.ETag = response.ETag;
            if (output.Tags == null)
            {
                output.Tags = new List<string>();
            }

            return output;
        }

        private IDictionary<string, DeploymentStatus> GetDeviceStatuses(string tenantId, Configuration deployment)
        {
            var deviceWithStatus = new Dictionary<string, DeploymentStatus>();
            IDictionary<string, long> metrics = deployment.SystemMetrics?.Results;

            bool appliedCountKeyExists = false;
            long appliedCount = 0;
            if (metrics.ContainsKey("appliedCount"))
            {
                appliedCountKeyExists = true;
                appliedCount = metrics["appliedCount"];
            }

            if ((appliedCountKeyExists && appliedCount > 0) || !appliedCountKeyExists)
            {
                string deploymentType = null;
                if (ConfigurationsHelper.IsEdgeDeployment(deployment))
                {
                    deploymentType = PackageType.EdgeManifest.ToString();
                }
                else
                {
                    deploymentType = PackageType.DeviceConfiguration.ToString();
                }

                deployment.Labels.TryGetValue(ConfigurationsHelper.ConfigTypeLabel, out string configType);
                var queries = DeviceStatusQueries.GetQueries(deploymentType, configType);

                string deploymentId = deployment.Id;

                var appliedDevices = this.GetDevicesInQuery(queries[QueryType.APPLIED], deploymentId, tenantId);

                if (!ConfigurationsHelper.IsEdgeDeployment(deployment) && !configType.Equals(ConfigType.Firmware.ToString()))
                {
                    foreach (var devices in appliedDevices)
                    {
                        deviceWithStatus.Add(devices, DeploymentStatus.Unknown);
                    }

                    return deviceWithStatus;
                }

                if (appliedDevices != null && appliedDevices.Count > 0)
                {
                    // Get reported status from custom Metrics if available otherwise use default queries
                    var successfulDevices = this.GetDevicesInQuery(deployment.Metrics.Queries.ContainsKey(SuccessQueryName) ? deployment.Metrics.Queries[SuccessQueryName] : queries[QueryType.SUCCESSFUL], deploymentId, tenantId);
                    var failedDevices = this.GetDevicesInQuery(deployment.Metrics.Queries.ContainsKey(FailedQueryName) ? deployment.Metrics.Queries[FailedQueryName] : queries[QueryType.FAILED], deploymentId, tenantId);

                    foreach (var device in appliedDevices)
                    {
                        if (successfulDevices.Contains(device))
                        {
                            deviceWithStatus.Add(device, DeploymentStatus.Succeeded);
                        }
                        else if (failedDevices.Contains(device))
                        {
                            deviceWithStatus.Add(device, DeploymentStatus.Failed);
                        }
                        else
                        {
                            deviceWithStatus.Add(device, DeploymentStatus.Pending);
                        }
                    }
                }
            }

            return deviceWithStatus;
        }

        private HashSet<string> GetDevicesInQuery(string hubQuery, string deploymentId, string tenantId)
        {
            var query = string.Format(hubQuery, deploymentId);
            var queryResponse = TenantConnectionHelper.GetRegistry(tenantId).CreateQuery(query);
            var deviceIds = new HashSet<string>();

            try
            {
                while (queryResponse.HasMoreResults)
                {
                    // TODO: Add pagination with queryOptions
                    var resultSet = queryResponse.GetNextAsJsonAsync();
                    foreach (var result in resultSet.Result)
                    {
                        var deviceId = JToken.Parse(result)[DeviceIdKey];
                        deviceIds.Add(deviceId.ToString());
                    }
                }
            }
            catch (Exception)
            {
                // this.logger.LogError(ex, "Error getting status of devices in query {query}", query);
            }

            return deviceIds;
        }

        private IDictionary<DeploymentStatus, long> CalculateDeviceMetrics(
            IDictionary<string,
            DeploymentStatus> deviceStatuses)
        {
            if (deviceStatuses == null)
            {
                return null;
            }

            IDictionary<DeploymentStatus, long> deviceMetrics = new Dictionary<DeploymentStatus, long>();

            deviceMetrics[DeploymentStatus.Succeeded] = deviceStatuses.Where(item =>
                                                            item.Value == DeploymentStatus.Succeeded).LongCount();

            deviceMetrics[DeploymentStatus.Failed] = deviceStatuses.Where(item =>
                                                            item.Value == DeploymentStatus.Failed).LongCount();

            deviceMetrics[DeploymentStatus.Pending] = deviceStatuses.Where(item =>
                                                            item.Value == DeploymentStatus.Pending).LongCount();

            return deviceMetrics;
        }

        private async Task<List<TwinServiceModel>> GetDeviceProperties(string tenantId, IEnumerable<string> deviceIds)
        {
            List<TwinServiceModel> twins = null;
            DeviceServiceListModel devices = null;
            string deviceQuery = @"deviceId IN [{0}]";

            if (deviceIds != null && deviceIds.Count() > 0)
            {
                var deviceIdsQuery = string.Join(",", deviceIds.Select(d => $"'{d}'"));
                var query = string.Format(deviceQuery, deviceIdsQuery);

                devices = await this.GetListAsync(tenantId, query, null);
                if (devices != null && devices.Items.Count() > 0)
                {
                    twins = new List<TwinServiceModel>();

                    twins.AddRange(devices.Items.Select(i => i.Twin));
                }
            }

            return twins;
        }

        private async Task<DeviceServiceListModel> GetListAsync(string tenantId, string query, string continuationToken)
        {
            var twins = await this.GetTwinByQueryAsync(
                tenantId,
                QueryPrefix,
                query,
                continuationToken,
                1000);

            var connectedEdgeDevices = await this.GetConnectedEdgeDevices(tenantId, twins.Result);

            var resultModel = new DeviceServiceListModel(
                twins.Result.Select(azureTwin => new DeviceServiceModel(
                    azureTwin,
                    string.Empty,
                    connectedEdgeDevices.ContainsKey(azureTwin.DeviceId))),
                twins.ContinuationToken);

            return resultModel;
        }

        private async Task<Dictionary<string, Twin>> GetConnectedEdgeDevices(string tenantId, List<Twin> twins)
        {
            var devicesWithConnectedModules = await this.GetDevicesWithConnectedModules(tenantId);
            var edgeTwins = twins
                .Where(twin => twin.Capabilities?.IotEdge ?? twin.Capabilities?.IotEdge ?? false)
                .Where(edgeDvc => devicesWithConnectedModules.Contains(edgeDvc.DeviceId))
                .ToDictionary(edgeDevice => edgeDevice.DeviceId, edgeDevice => edgeDevice);
            return edgeTwins;
        }

        private async Task<HashSet<string>> GetDevicesWithConnectedModules(string tenantId)
        {
            var connectedEdgeDevices = new HashSet<string>();

            var edgeModules = await this.GetModuleTwinsByQueryAsync(tenantId, DevicesConnectedQuery, string.Empty);
            foreach (var model in edgeModules.Items)
            {
                connectedEdgeDevices.Add(model.DeviceId);
            }

            return connectedEdgeDevices;
        }

        private async Task<ResultWithContinuationToken<List<Twin>>> GetTwinByQueryAsync(
            string tenantId,
            string queryPrefix,
            string query,
            string continuationToken,
            int numberOfResult)
        {
            query = string.IsNullOrEmpty(query) ? queryPrefix : $"{queryPrefix} where {query}";

            var twins = new List<Twin>();

            var twinQuery = TenantConnectionHelper.GetRegistry(tenantId).CreateQuery(query);

            QueryOptions options = new QueryOptions();
            options.ContinuationToken = continuationToken;

            while (twinQuery.HasMoreResults && twins.Count < numberOfResult)
            {
                var response = await twinQuery.GetNextAsTwinAsync(options);
                options.ContinuationToken = response.ContinuationToken;
                twins.AddRange(response);
            }

            return new ResultWithContinuationToken<List<Twin>>(twins, options.ContinuationToken);
        }

        private class ResultWithContinuationToken<T>
        {
            public ResultWithContinuationToken(T queryResult, string continuationToken)
            {
                this.Result = queryResult;
                this.ContinuationToken = continuationToken;
            }

            public T Result { get; private set; }

            public string ContinuationToken { get; private set; }
        }
    }
}