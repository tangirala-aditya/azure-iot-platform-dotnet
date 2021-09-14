// <copyright file="DeploymentSyncService.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.CodeAnalysis.CodeActions;
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
        private const int DeviceStatusLength = 10000;
        private const string DeviceStatusesCollection = "deviceStatuses-{0}";
        private const string QueryPrefix = "SELECT * FROM devices";
        private const string ModuleQueryPrefix = "SELECT * FROM devices.modules";
        private const string DevicesConnectedQuery = "connectionState = 'Connected'";
        private const string DeploymentsCollection = "deployments";
        private const string CollectionKey = "pcs";
        private const string DeploymentEdgeModulePropertiesCollection = "deploymentedgemodules-{0}";
        private const string DeploymentModuleHistoryPropertiesCollection = "deploymentModulesHistory-{0}_{1}";
        private const string DeploymentHistoryCollection = "deviceDeploymentHistory-{0}";

        private FeedOptions DefaultQueryOptions => new FeedOptions
        {
            EnableCrossPartitionQuery = true,
            EnableScanInQuery = true,
        };

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
                await this.SaveDeviceStatuses(devicesStatuses, deployment.Id, tenantId);
                deployment.DeploymentMetrics.DeviceMetrics = this.CalculateDeviceMetrics(devicesStatuses);
            }

            await this.SaveDeployment(deployment, tenantId);

            if (devicesStatuses != null && devicesStatuses.Keys.Count > 0)
            {
                if (ConfigurationsHelper.IsEdgeDeployment(configuration))
                {
                    var deviceTwins = await this.GetDeviceProperties(tenantId, deployment);
                    if (deviceTwins != null && deviceTwins.Count > 0)
                    {
                        await this.SaveDeploymentFromHub(tenantId, deployment, deviceTwins);
                    }

                    var moduleTwins = await this.GetDeviceProperties(tenantId, deployment, PackageType.EdgeManifest);
                    await this.StoreModuleTwinsInStorage(tenantId, moduleTwins, deployment.Id);
                }
                else
                {
                    bool deviceDeploymentTwins = await this.DoesDeploymentTwinsExist(tenantId, deployment.Id);
                    if (!deviceDeploymentTwins)
                    {
                        var deviceTwins = await this.GetDeviceProperties(tenantId, deployment);
                        if (deviceTwins != null && deviceTwins.Count > 0)
                        {
                            await this.SaveDeploymentFromHub(tenantId, deployment, deviceTwins);
                        }
                    }
                }
            }

            return deployment;
        }

        public async Task<List<DeploymentServiceModel>> GetDeploymentsByIdFromStorage(string tenantId, string[] deploymentIds)
        {
            var sql = QueryBuilder.GetDocumentsSql(
                "deployments",
                null,
                null,
                null,
                "_ts",
                null,
                "_ts",
                "desc",
                "_ts",
                0,
                100,
                deploymentIds,
                "Key");

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

        public async Task SaveDeploymentHistory(string tenantId, DeploymentServiceModel deploymentModel, Twin deviceTwin)
        {
            CosmosOperations storageClient = await CosmosOperations.GetClientAsync();

            TwinServiceModel previousTwin = await this.GetPreviousFirmwareReportedProperties(tenantId, deviceTwin.DeviceId);
            var deviceTwinServiceModel = new TwinServiceModel(deviceTwin);
            DeploymentHistoryModel modelToSave = new DeploymentHistoryModel
            {
                DeploymentId = deploymentModel.Id,
                DeploymentName = deploymentModel.Name,
                DeviceId = deviceTwin.DeviceId,
                PreviousFirmwareTwin = previousTwin,
                LastUpdatedDateTimeUtc = DateTime.UtcNow,
                Twin = deviceTwinServiceModel,
            };

            var value = JsonConvert.SerializeObject(
                                        modelToSave,
                                        Formatting.None,
                                        new JsonSerializerSettings
                                        {
                                            NullValueHandling = NullValueHandling.Ignore,
                                        });

            await storageClient.SaveDocumentAsync(string.Format(DeploymentHistoryCollection, deviceTwin.DeviceId), deploymentModel.Id, new ValueServiceModel() { Data = value }, this.GenerateCollectionLink(tenantId), Guid.NewGuid());
        }

        public async Task SaveDeploymentFromHub(string tenantId, DeploymentServiceModel deploymentModel, List<TwinServiceModel> deviceTwins)
        {
            CosmosOperations storageClient = await CosmosOperations.GetClientAsync();

            foreach (TwinServiceModel deviceTwin in deviceTwins)
            {
                DeploymentHistoryModel modelToSave = new DeploymentHistoryModel
                {
                    DeploymentId = deploymentModel.Id,
                    DeploymentName = deploymentModel.Name,
                    DeviceId = deviceTwin.DeviceId,
                    PreviousFirmwareTwin = null,
                    LastUpdatedDateTimeUtc = DateTime.UtcNow,
                    Twin = deviceTwin,
                };

                var value = JsonConvert.SerializeObject(
                                            modelToSave,
                                            Formatting.None,
                                            new JsonSerializerSettings
                                            {
                                                NullValueHandling = NullValueHandling.Ignore,
                                            });

                await storageClient.SaveDocumentAsync(string.Format(DeploymentHistoryCollection, deviceTwin.DeviceId), deploymentModel.Id, new ValueServiceModel() { Data = value }, this.GenerateCollectionLink(tenantId), Guid.NewGuid());
            }
        }

        private async Task<TwinServiceModel> GetPreviousFirmwareReportedProperties(string tenantId, string deviceId)
        {
            var sql = QueryBuilder.GetDeploymentDeviceDocumentsSqlByKey("CollectionId", $"deviceDeploymentHistory-{deviceId}");

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
                   100);

                var result = docs.Select(doc => new ValueServiceModel(doc));
                if (result != null && result.Count() > 0)
                {
                    var previousDeployment = JsonConvert.DeserializeObject<DeploymentHistoryModel>(result.FirstOrDefault()?.Data);
                    var previousTwin = previousDeployment.Twin;
                    return previousTwin != null ? previousTwin : null;
                }

                return null;
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No deployments exist in CosmosDb. The telemetry collection {$"pcs-{tenantId}"} does not exist.", e);
            }
        }

        private async Task<bool> DoesDeploymentTwinsExist(string tenantId, string deploymentId)
        {
            var sql = QueryBuilder.GetDeploymentDeviceDocumentsSqlByKey("Key", deploymentId);
            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                EnableScanInQuery = true,
            };

            CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
            List<Document> docs = new List<Document>();
            docs = await storageClient.QueryDocumentsAsync(
               "pcs-storage",
               $"pcs-{tenantId}",
               this.DefaultQueryOptions,
               sql,
               0,
               1);
            return docs != null && docs.Count > 0;
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

        private async Task<TwinServiceListModel> GetTwins(string tenantId, string collectionId)
        {
            var sql = CosmosOperations.GetDocumentsByCollectionId("CollectionId", collectionId);

            List<Document> docs = new List<Document>();

            try
            {
                CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
                docs = await storageClient.QueryAllDocumentsAsync(
                   "pcs-storage",
                   $"pcs-{tenantId}",
                   this.DefaultQueryOptions,
                   sql);

                var result = docs == null ?
                    new List<TwinServiceModel>() :
                    docs
                        .Select(doc => new ValueServiceModel(doc)).Select(x => JsonConvert.DeserializeObject<TwinServiceModel>(x.Data))
                        .ToList();
                return new TwinServiceListModel(result, null);
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No records exist in CosmosDb. The CollectionId {collectionId} does not exist.", e);
            }
        }

        private async Task<IDictionary<string, DeploymentStatus>> FetchDeviceStatuses(string tenantId, string deploymentId)
        {
            CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
            var sql = CosmosOperations.GetDocumentsByCollectionId("CollectionId", string.Format(DeviceStatusesCollection, deploymentId));
            var statuses = new Dictionary<string, DeploymentStatus>();

            var existingDeviceStatuses = await storageClient.QueryAllDocumentsAsync(
                  "pcs-storage",
                  $"pcs-{tenantId}",
                  this.DefaultQueryOptions,
                  sql);

            if (existingDeviceStatuses != null && existingDeviceStatuses.Count > 0)
            {
                foreach (var item in existingDeviceStatuses.Select(doc => new ValueServiceModel(doc)))
                {
                    statuses = statuses.Union(JsonConvert.DeserializeObject<DeviceStatusServiceModel>(item.Data).DeviceStatuses).ToDictionary(k => k.Key, v => v.Value);
                }
            }

            return statuses;
        }

        private async Task SaveDeviceStatuses(IDictionary<string, DeploymentStatus> deviceStatuses, string deploymentId, string tenantId)
        {
            CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
            var sql = CosmosOperations.GetDocumentsByCollectionId("CollectionId", string.Format(DeviceStatusesCollection, deploymentId));

            var existingDeviceStatuses = await storageClient.QueryAllDocumentsAsync(
                  "pcs-storage",
                  $"pcs-{tenantId}",
                  this.DefaultQueryOptions,
                  sql);

            if (existingDeviceStatuses != null && existingDeviceStatuses.Count > 0)
            {
                foreach (var item in existingDeviceStatuses)
                {
                    await storageClient.DeleteDocumentAsync(item.Id, this.GenerateCollectionLink(tenantId));
                }
            }

            if (deviceStatuses != null)
            {
                for (int i = 0; i < deviceStatuses.Count; i = i + DeviceStatusLength)
                {
                    var items = deviceStatuses.Skip(i).Take(DeviceStatusLength).ToDictionary(p => p.Key, p => p.Value);
                    var value = JsonConvert.SerializeObject(
                                                        new DeviceStatusServiceModel
                                                        {
                                                            DeviceStatuses = items,
                                                            DeploymentId = deploymentId,
                                                        },
                                                        Formatting.Indented,
                                                        new JsonSerializerSettings
                                                        {
                                                            NullValueHandling = NullValueHandling.Ignore,
                                                        });
                    await storageClient.SaveDocumentAsync(string.Format(DeviceStatusesCollection, deploymentId), Guid.NewGuid().ToString(), new ValueServiceModel() { Data = value }, this.GenerateCollectionLink(tenantId));
                }
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

        private async Task SaveModuleTwin(string tenantId, string deploymentId, TwinServiceModel moduleTwin)
        {
            CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
            var value = JsonConvert.SerializeObject(
                moduleTwin,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });

            await storageClient.SaveDocumentAsync(string.Format(DeploymentEdgeModulePropertiesCollection, deploymentId), $"{moduleTwin.DeviceId}-{this.RemoveSpecialCharacters(moduleTwin.ModuleId)}", new ValueServiceModel() { Data = value }, this.GenerateCollectionLink(tenantId));
        }

        private async Task StoreModuleTwinsInStorage(string tenantId, List<TwinServiceModel> moduleTwins, string deploymentId)
        {
            if (moduleTwins != null && moduleTwins.Count > 0)
            {
                TwinServiceListModel existingModuleTwins = await this.GetTwins(tenantId, string.Format(DeploymentEdgeModulePropertiesCollection, deploymentId));
                if (existingModuleTwins == null || (existingModuleTwins != null && existingModuleTwins.Items.Count == 0))
                {
                    foreach (var moduleTwin in moduleTwins)
                    {
                        await this.SaveModuleTwin(tenantId, deploymentId, moduleTwin);
                    }
                }
                else
                {
                    foreach (var moduleTwin in moduleTwins)
                    {
                        var existingModuleTwin = existingModuleTwins.Items.FirstOrDefault(x => x.ModuleId == moduleTwin.ModuleId && x.DeviceId == moduleTwin.DeviceId);
                        CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
                        var value = JsonConvert.SerializeObject(
                        moduleTwin,
                        Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                        });

                        try
                        {
                            await storageClient.SaveDocumentAsync(string.Format(DeploymentEdgeModulePropertiesCollection, deploymentId), $"{moduleTwin.DeviceId}-{this.RemoveSpecialCharacters(moduleTwin.ModuleId)}", new ValueServiceModel() { Data = value, ETag = existingModuleTwin.ETag }, this.GenerateCollectionLink(tenantId));
                        }
                        catch (Exception)
                        {
                        }

                        // archive exisiting Device Twin
                        var archiveModuleTwinValue = JsonConvert.SerializeObject(
                            existingModuleTwin,
                            Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                            });
                        try
                        {
                            await storageClient.SaveDocumentAsync(string.Format(DeploymentModuleHistoryPropertiesCollection, deploymentId, Guid.NewGuid().ToString()), $"{moduleTwin.DeviceId}-{this.RemoveSpecialCharacters(moduleTwin.ModuleId)}", new ValueServiceModel() { Data = archiveModuleTwinValue }, this.GenerateCollectionLink(tenantId));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        private string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            var validCharacters = "_-";
            foreach (char c in str)
            {
                if (char.IsLetterOrDigit(c) || validCharacters.Contains(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
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

        private async Task<List<TwinServiceModel>> GetDeviceProperties(string tenantId, DeploymentServiceModel deploymentDetail, PackageType packageType = PackageType.DeviceConfiguration)
        {
            List<TwinServiceModel> twins = new List<TwinServiceModel>();
            if (packageType == PackageType.EdgeManifest)
            {
                IEnumerable<string> deviceIds = deploymentDetail.DeploymentMetrics.DeviceStatuses != null ? deploymentDetail.DeploymentMetrics.DeviceStatuses.Keys : this.FetchDeviceStatuses(tenantId, deploymentDetail.Id).Result.Keys;
                if (deviceIds != null && deviceIds.Count() > 0)
                {
                    string moduleQuery = @"deviceId IN [{0}] AND moduleId = '$edgeAgent'";
                    var deviceIdsQuery = string.Join(",", deviceIds.Select(d => $"'{d}'"));
                    var query = string.Format(moduleQuery, deviceIdsQuery);

                    await this.GetModuleTwins(tenantId, query, null, twins);
                }
            }
            else
            {
                string deviceQuery = deploymentDetail.TargetCondition == "*" ? string.Empty : deploymentDetail.TargetCondition;
                await this.GetDeviceTwins(tenantId, deviceQuery, null, twins);
            }

            return twins;
        }

        private async Task GetDeviceTwins(string tenantId, string query, string continuationToken, List<TwinServiceModel> twins)
        {
            DeviceServiceListModel devices = null;
            devices = await this.GetListAsync(tenantId, query, continuationToken);

            if (devices != null && devices.Items.Count() > 0)
            {
                twins.AddRange(devices.Items.Select(i => i.Twin));
                if (!string.IsNullOrWhiteSpace(devices.ContinuationToken))
                {
                    await this.GetDeviceTwins(tenantId, query, devices.ContinuationToken, twins);
                }
            }
        }

        private async Task GetModuleTwins(string tenantId, string query, string continuationToken, List<TwinServiceModel> twins)
        {
            TwinServiceListModel moduleTwins = null;
            moduleTwins = await this.GetModuleTwinsByQueryAsync(tenantId, query, continuationToken);

            if (moduleTwins != null && moduleTwins.Items.Count() > 0)
            {
                twins.AddRange(moduleTwins.Items);
                if (!string.IsNullOrWhiteSpace(moduleTwins.ContinuationToken))
                {
                    await this.GetModuleTwins(tenantId, query, moduleTwins.ContinuationToken, twins);
                }
            }
        }

        private async Task<DeviceServiceListModel> GetListAsync(string tenantId, string query, string continuationToken)
        {
            var twins = await this.GetTwinByQueryAsync(
                tenantId,
                QueryPrefix,
                query,
                continuationToken,
                MaximumGetList);

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

        private async Task<TwinServiceListModel> GetModuleTwinsByQueryAsync(
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