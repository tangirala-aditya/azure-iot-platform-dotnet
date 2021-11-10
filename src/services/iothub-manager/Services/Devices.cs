// <copyright file="Devices.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.External.KustoStorage;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.IoTHubManager.Services.Extensions;
using Mmm.Iot.IoTHubManager.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.StorageAdapter.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AuthenticationType = Mmm.Iot.IoTHubManager.Services.Models.AuthenticationType;
using QueryConditionTranslator = Mmm.Iot.IoTHubManager.Services.Helpers.QueryConditionTranslator;

namespace Mmm.Iot.IoTHubManager.Services
{
    public delegate Task<DevicePropertyServiceModel> DevicePropertyDelegate(DevicePropertyServiceModel model);

    public class Devices : IDevices
    {
        private const int MaximumGetList = 1000;
        private const string QueryPrefix = "SELECT * FROM devices";
        private const string KustoQueryPrefix = "DeviceTwin | summarize arg_max(TimeStamp, *) by DeviceId | where IsDeleted == false";
        private const string KustoOrderByQuery = "| order by DeviceCreatedDate desc nulls last";
        private const string ModuleQueryPrefix = "SELECT * FROM devices.modules";
        private const string DeviceConnectionStateCountQueryPrefix = "SELECT COUNT() AS numberOfDevices, connectionState FROM devices";
        private const string DeviceConnectionStateCountKustoQuery = "| summarize numberOfDevices = count() by connectionState = tostring(Twin[\"connectionState\"])";
        private const string DeviceConnectionState = "connectionState";
        private const string DevicesConnectedQuery = "connectionState = 'Connected'";
        private const string TwinChangeDatabase = "iot";
        private const string AppConfigTenantInfoKey = "tenant";
        private const string AppConfigLifecycleCollectionKey = "lifecycle-collection";
        private const string DeviceLinkingJobsCollection = "devicelinkingjobs-{0}";
        private readonly ITenantConnectionHelper tenantConnectionHelper;
        private readonly IAsaManagerClient asaManager;
        private readonly IDeviceQueryCache deviceQueryCache;
        private readonly IStorageClient storageClient;
        private readonly IAppConfigurationClient appConfigurationClient;
        private readonly IKustoQueryClient kustoQueryClient;
        private readonly IStorageAdapterClient storageAdapterclient;
        private readonly AppConfig config;
        private readonly bool kustoEnabled;

        public Devices(
            AppConfig config,
            ITenantConnectionHelper tenantConnectionHelper,
            IAsaManagerClient asaManagerClient,
            IDeviceQueryCache deviceQueryCache,
            IStorageClient storageClient,
            IAppConfigurationClient appConfigurationClient,
            IKustoQueryClient kustoQueryClient,
            IStorageAdapterClient storageAdapterclient)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.kustoEnabled = config.DeviceTelemetryService.Messages.TelemetryStorageType.Equals(
                TelemetryStorageTypeConstants.Ade, StringComparison.OrdinalIgnoreCase);

            this.kustoQueryClient = kustoQueryClient;
            this.tenantConnectionHelper = tenantConnectionHelper;
            this.asaManager = asaManagerClient;
            this.deviceQueryCache = deviceQueryCache;
            this.storageClient = storageClient;
            this.appConfigurationClient = appConfigurationClient;
            this.storageAdapterclient = storageAdapterclient;
            this.config = config;
        }

        public Devices(
            ITenantConnectionHelper tenantConnectionHelper,
            string ioTHubHostName,
            IAsaManagerClient asaManagerClient,
            IDeviceQueryCache deviceQueryCache,
            IStorageClient storageClient,
            IStorageAdapterClient storageAdapterclient)
        {
            this.tenantConnectionHelper = tenantConnectionHelper ?? throw new ArgumentNullException("tenantConnectionHelper " + ioTHubHostName);
            this.asaManager = asaManagerClient;
            this.deviceQueryCache = deviceQueryCache;
            this.storageClient = storageClient;
            this.storageAdapterclient = storageAdapterclient;
        }

        public virtual string DocumentDataType
        {
            get
            {
                return "pcs";
            }
        }

        public virtual string DocumentDatabaseSuffix
        {
            get
            {
                return "storage";
            }
        }

        public virtual string DocumentDbDatabaseId
        {
            get
            {
                return $"{this.DocumentDataType}-{this.DocumentDatabaseSuffix}";
            }
        }

        // Ping the registry to see if the connection is healthy
        public async Task<StatusResultServiceModel> StatusAsync()
        {
            var result = new StatusResultServiceModel(false, string.Empty);
            try
            {
                await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync("healthcheck");
                result.IsHealthy = true;
                result.Message = "Alive and Well!";
            }
            catch (Exception e)
            {
                result.Message = e.Message;
            }

            return result;
        }

        public async Task<DeviceServiceListModel> GetListAsync(string inputQuery, string continuationToken)
        {
            if (this.kustoEnabled)
            {
                return await this.GetListFromADXAsync(inputQuery);
            }

            return await this.GetListFromIoTHubAsync(inputQuery, continuationToken);
        }

        public async Task<DeviceServiceListModel> GetListFromIoTHubAsync(string inputQuery, string continuationToken)
        {
            string querytoBeCached = inputQuery;
            IEnumerable<QueryConditionClause> clauses = null;
            IEnumerable<QueryConditionClause> deviceIdClauses = null;
            if (!string.IsNullOrWhiteSpace(inputQuery))
            {
                try
                {
                    clauses = JsonConvert.DeserializeObject<IEnumerable<QueryConditionClause>>(inputQuery);
                    deviceIdClauses = clauses.Where(x => x.Key == "deviceId" && x.Operator == "LK").ToList();

                    if (deviceIdClauses != null && deviceIdClauses.Count() > 0)
                    {
                        clauses = clauses.Where(x => x.Key != "deviceId" && x.Operator != "LK");
                        inputQuery = JsonConvert.SerializeObject(clauses);
                    }
                }
                catch
                {
                    // Any exception raised in deserializing will be ignored
                }

                if (!string.IsNullOrWhiteSpace(inputQuery))
                {
                    // Try to translate clauses to query
                    inputQuery = QueryConditionTranslator.ToQueryString(inputQuery);
                }
            }

            DeviceServiceListModel resultModel = null;
            string tenantId = this.tenantConnectionHelper.TenantId;

            if (string.IsNullOrWhiteSpace(continuationToken))
            {
                resultModel = await this.deviceQueryCache.GetCachedQueryResultAsync(tenantId, querytoBeCached);

                if (resultModel != null)
                {
                    return resultModel;
                }
            }

            string query = string.Empty;
            int iotHublimit = 500;
            string deviceListValue = string.Empty;
            ResultWithContinuationToken<List<Twin>> allTwins = new ResultWithContinuationToken<List<Twin>>(new List<Twin>(), continuationToken);
            if (deviceIdClauses != null && deviceIdClauses.Count() > 0)
            {
                foreach (var deviceIdClause in deviceIdClauses)
                {
                    List<string> deviceIds = await this.GetDevicesBasedOnInputDeviceString(deviceIdClause.Value.ToString().ToLower(), tenantId);
                    for (int i = 0; i < (deviceIds.Count / iotHublimit) + 1; i++)
                    {
                        if (i != 0 && (deviceIds.Count % (i * iotHublimit)) <= 0)
                        {
                            break;
                        }

                        List<string> batchDeviceIds = deviceIds.Skip(i * iotHublimit).Take(iotHublimit).ToList();
                        if (batchDeviceIds != null && batchDeviceIds.Count > 0)
                        {
                            // deviceListValue = $"({string.Join(" or ", deviceIds.Select(v => $"deviceId = '{v}'"))})";
                            deviceListValue = string.Join(",", batchDeviceIds.Select(p => $"'{p}'"));
                        }

                        if (!string.IsNullOrWhiteSpace(inputQuery))
                        {
                            // Try to translate clauses to query
                            query = $"{inputQuery} AND deviceId IN [{deviceListValue}]";
                        }
                        else
                        {
                            query = $" deviceId IN [{deviceListValue}]";
                        }

                        int countOfDevicestoFetch = string.IsNullOrWhiteSpace(deviceListValue) ? MaximumGetList : deviceIds.Count();

                        var twins = await this.GetTwinByQueryAsync(
                            QueryPrefix,
                            query,
                            continuationToken,
                            countOfDevicestoFetch);

                        allTwins.Result.AddRange(twins.Result.Except(allTwins.Result));
                        while (!string.IsNullOrWhiteSpace(twins.ContinuationToken))
                        {
                            twins = await this.GetTwinByQueryAsync(
                            QueryPrefix,
                            query,
                            continuationToken,
                            countOfDevicestoFetch);
                            allTwins.Result.AddRange(twins.Result);
                        }
                    }
                }
            }
            else
            {
                allTwins = await this.GetTwinByQueryAsync(
                QueryPrefix,
                inputQuery,
                continuationToken,
                MaximumGetList);
            }

            var connectedEdgeDevices = await this.GetConnectedEdgeDevices(allTwins.Result);

            var parentDevices = await this.GetParentDevices(allTwins.Result);

            resultModel = new DeviceServiceListModel(
                allTwins.Result.Select(azureTwin => new DeviceServiceModel(
                    azureTwin,
                    this.tenantConnectionHelper.GetIotHubName(),
                    connectedEdgeDevices.ContainsKey(azureTwin.DeviceId),
                    this.GetParentDevice(azureTwin, parentDevices))),
                allTwins.ContinuationToken);

            if (string.IsNullOrWhiteSpace(continuationToken))
            {
                this.deviceQueryCache.SetTenantQueryResult(
                    this.tenantConnectionHelper.TenantId,
                    querytoBeCached,
                    new DeviceQueryCacheResultServiceModel
                    {
                        Result = resultModel,
                        ResultTimestamp = DateTimeOffset.Now,
                    });
            }

            return resultModel;
        }

        public async Task<DeviceServiceListModel> GetListFromADXAsync(string inputQuery)
        {
            string querytoBeCached = inputQuery;

            if (!string.IsNullOrWhiteSpace(inputQuery))
            {
                // Try to translate clauses to query
                inputQuery = QueryConditionTranslator.ToADXQueryString(inputQuery);
            }

            DeviceServiceListModel resultModel = null;

            // Commented cache.
            // string tenantId = this.tenantConnectionHelper.TenantId;
            // resultModel = await this.deviceQueryCache.GetCachedQueryResultAsync(tenantId, querytoBeCached);
            // if (resultModel != null)
            // {
            //     return resultModel;
            // }
            string query = string.Empty;
            string deviceListValue = string.Empty;
            var allTwins = await this.GetTwinDataADXQueryAsync<DeviceTwinMirrorModel>(
                KustoQueryPrefix,
                inputQuery,
                KustoOrderByQuery);

            var connectedEdgeDevices = await this.GetConnectedEdgeDevices(allTwins.Result.Select(x => x.Twin).ToList());
            resultModel = new DeviceServiceListModel(
                allTwins.Result.Select(azureTwin => new DeviceServiceModel(
                    azureTwin.Twin,
                    this.tenantConnectionHelper.GetIotHubName(),
                    connectedEdgeDevices.ContainsKey(azureTwin.DeviceId),
                    azureTwin.DeviceCreatedDate,
                    azureTwin.TimeStamp)),
                allTwins.ContinuationToken);

            // Commented cache.
            // this.deviceQueryCache.SetTenantQueryResult(
            //    this.tenantConnectionHelper.TenantId,
            //    querytoBeCached,
            //    new DeviceQueryCacheResultServiceModel
            //    {
            //        Result = resultModel,
            //        ResultTimestamp = DateTimeOffset.Now,
            //    });
            return resultModel;
        }

        public async Task<DeviceServiceListModel> GetDeviceListAsync(string query, string continuationToken)
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                // Try to translate clauses to query
                query = QueryConditionTranslator.ToQueryString(query);
            }

            var twins = await this.GetTwinByQueryAsync(
                QueryPrefix,
                query,
                continuationToken,
                MaximumGetList);

            var connectedEdgeDevices = await this.GetConnectedEdgeDevices(twins.Result);

            var parentDevices = await this.GetParentDevices(twins.Result);

            var resultModel = new DeviceServiceListModel(
                twins.Result.Select(azureTwin => new DeviceServiceModel(
                    azureTwin,
                    this.tenantConnectionHelper.GetIotHubName(),
                    connectedEdgeDevices.ContainsKey(azureTwin.DeviceId),
                    this.GetParentDevice(azureTwin, parentDevices))),
                twins.ContinuationToken);

            return resultModel;
        }

        public async Task<DeviceTwinName> GetDeviceTwinNamesAsync()
        {
            var content = await this.GetListAsync(string.Empty, string.Empty);

            return content.GetDeviceTwinNames();
        }

        public async Task<DeviceServiceModel> GetAsync(string id)
        {
            var device = this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(id);
            var twin = this.tenantConnectionHelper.GetRegistry().GetTwinAsync(id);

            await Task.WhenAll(device, twin);

            if (device.Result == null)
            {
                throw new ResourceNotFoundException("The device doesn't exist.");
            }

            var isEdgeConnectedDevice = await this.DoesDeviceHaveConnectedModules(device.Result.Id);

            return new DeviceServiceModel(device.Result, twin.Result, this.tenantConnectionHelper.GetIotHubName(), isEdgeConnectedDevice);
        }

        public async Task<DeviceServiceModel> CreateAsync(DeviceServiceModel device)
        {
            if (device.IsEdgeDevice &&
                device.Authentication != null &&
                !device.Authentication.AuthenticationType.Equals(AuthenticationType.Sas))
            {
                throw new InvalidInputException("Edge devices only support symmetric key authentication.");
            }

            // auto generate DeviceId, if missing
            if (string.IsNullOrEmpty(device.Id))
            {
                device.Id = Guid.NewGuid().ToString();
            }

            var azureDevice = await this.tenantConnectionHelper.GetRegistry().AddDeviceAsync(device.ToAzureModel());

            Twin azureTwin;
            if (device.Twin == null)
            {
                azureTwin = await this.tenantConnectionHelper.GetRegistry().GetTwinAsync(device.Id);
            }
            else
            {
                azureTwin = await this.tenantConnectionHelper.GetRegistry().UpdateTwinAsync(device.Id, device.Twin.ToAzureModel(), "*");
            }

            await this.asaManager.BeginDeviceGroupsConversionAsync();
            return new DeviceServiceModel(azureDevice, azureTwin, this.tenantConnectionHelper.GetIotHubName());
        }

        public async Task<DeviceServiceModel> UpdateAsync(DeviceServiceModel device, DevicePropertyDelegate devicePropertyDelegate)
        {
            // validate device module
            var azureDevice = await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(device.Id);
            if (azureDevice == null)
            {
                throw new ResourceNotFoundException($"Device {device.Id} could not be found on this tenant's IoT Hub. You must create the device first before calling the update method.");
            }

            Twin azureTwin;
            if (device.Twin == null)
            {
                azureTwin = await this.tenantConnectionHelper.GetRegistry().GetTwinAsync(device.Id);
            }
            else
            {
                azureTwin = await this.tenantConnectionHelper.GetRegistry().UpdateTwinAsync(device.Id, device.Twin.ToAzureModel(), device.Twin.ETag);

                // Update the deviceGroupFilter cache, no need to wait
                var model = new DevicePropertyServiceModel();

                if (JsonConvert.DeserializeObject(JsonConvert.SerializeObject(device.Twin.Tags)) is JToken tagRoot)
                {
                    model.Tags = new HashSet<string>(tagRoot.GetAllLeavesPath());
                }

                if (JsonConvert.DeserializeObject(JsonConvert.SerializeObject(device.Twin.ReportedProperties)) is JToken reportedRoot)
                {
                    model.Reported = new HashSet<string>(reportedRoot.GetAllLeavesPath());
                }

                _ = devicePropertyDelegate(model);
            }

            await this.asaManager.BeginDeviceGroupsConversionAsync();
            return new DeviceServiceModel(azureDevice, azureTwin, this.tenantConnectionHelper.GetIotHubName());
        }

        public async Task DeleteAsync(string id)
        {
            await this.tenantConnectionHelper.GetRegistry().RemoveDeviceAsync(id);
        }

        public async Task<TwinServiceModel> GetModuleTwinAsync(string deviceId, string moduleId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new InvalidInputException("A valid deviceId must be provided.");
            }

            if (string.IsNullOrWhiteSpace(moduleId))
            {
                throw new InvalidInputException("A valid moduleId must be provided.");
            }

            var twin = await this.tenantConnectionHelper.GetRegistry().GetTwinAsync(deviceId, moduleId);
            return new TwinServiceModel(twin);
        }

        public async Task<TwinServiceListModel> GetModuleTwinsByQueryAsync(
            string query,
            string continuationToken)
        {
            var twins = await this.GetTwinByQueryAsync(
                ModuleQueryPrefix,
                query,
                continuationToken,
                MaximumGetList);
            var result = twins.Result.Select(twin => new TwinServiceModel(twin)).ToList();

            return new TwinServiceListModel(result, twins.ContinuationToken);
        }

        /*
        public async Task<List<ModuleServiceModel>> GetModuleTwinsByDeviceIdAsync(string deviceId)
        {
            var modules = new List<ModuleServiceModel>();

            modules.AddRange(await this.GetModuleTwinsByDeviceIdAsync(deviceId));

            return modules;
        }
        */
        public async Task<ModuleServiceListModel> GetModuleTwinsByDeviceIdAsync(string deviceId, string continuationToken = null)
        {
            var modules = new List<ModuleServiceModel>();

            string query = $"deviceId = '{deviceId}'";

            var twins = await this.GetTwinByQueryAsync(
                ModuleQueryPrefix,
                query,
                continuationToken,
                MaximumGetList);

            if (twins != null && twins.Result.Count > 0)
            {
                modules.AddRange(twins.Result.Select(x => new ModuleServiceModel(x)));

                if (!string.IsNullOrWhiteSpace(twins.ContinuationToken))
                {
                    var modulesFromQuery = await this.GetModuleTwinsByDeviceIdAsync(deviceId, twins.ContinuationToken);

                    modules.AddRange(modulesFromQuery.Items);
                }
            }

            var edgeAgent = modules.FirstOrDefault(x => x.ModuleId == "$edgeAgent");
            Dictionary<string, string> moduleStatuses = new Dictionary<string, string>();
            if (edgeAgent != null && edgeAgent.Twin != null && edgeAgent.Twin.ReportedProperties.Count > 0)
            {
                var systemModules = edgeAgent.Twin.ReportedProperties["systemModules"];
                foreach (var item in systemModules.Children())
                {
                    var property = item as Newtonsoft.Json.Linq.JProperty;
                    moduleStatuses.Add(property.Name, property.Value["runtimeStatus"].ToString());
                }

                if (edgeAgent.Twin.ReportedProperties.ContainsKey("modules"))
                {
                    var customModules = edgeAgent.Twin.ReportedProperties["modules"];
                    foreach (var item in customModules.Children())
                    {
                        var property = item as Newtonsoft.Json.Linq.JProperty;
                        moduleStatuses.Add(property.Name, property.Value["runtimeStatus"].ToString());
                    }
                }
            }

            foreach (var moduleId in moduleStatuses.Keys)
            {
                if (moduleId == "edgeAgent" || moduleId == "edgeHub")
                {
                    var module = modules.FirstOrDefault(x => x.ModuleId == $"${moduleId}");
                    if (module != null)
                    {
                        module.Status = moduleStatuses[moduleId];
                    }
                }
                else
                {
                    var module = modules.FirstOrDefault(x => x.ModuleId == moduleId);
                    if (module != null)
                    {
                        module.Status = moduleStatuses[moduleId];
                    }
                }
            }

            return new ModuleServiceListModel(modules);
        }

        public async Task<DeploymentHistoryListModel> GetDeploymentHistoryAsync(string deviceId, string tenantId)
        {
            var sql = QueryBuilder.GetDeviceDocumentsSqlByKey($"deviceDeploymentHistory-{deviceId}", "CollectionId");
            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                EnableScanInQuery = true,
            };
            List<Document> docs = await this.storageClient.QueryDocumentsAsync(
                this.DocumentDbDatabaseId,
                $"{this.DocumentDataType}-{tenantId}",
                queryOptions,
                sql,
                0,
                1000);

            return docs == null
                 ? new DeploymentHistoryListModel(null)
                 : new DeploymentHistoryListModel(docs
                    .Select(doc => new ValueServiceModel(doc)).Select(x => JsonConvert.DeserializeObject<DeploymentHistoryModel>(x.Data))
                    .ToList());
        }

        public async Task<DeviceStatisticsServiceModel> GetDeviceStatisticsAsync(string query)
        {
            ResultWithContinuationToken<List<DeviceConnectionStatusCountModel>> data = null;

            if (this.kustoEnabled)
            {
                if (!string.IsNullOrWhiteSpace(query))
                {
                    // Try to translate clauses to query
                    query = QueryConditionTranslator.ToADXQueryString(query);
                }

                data = await this.GetTwinDataADXQueryAsync<DeviceConnectionStatusCountModel>(
                    KustoQueryPrefix,
                    query,
                    DeviceConnectionStateCountKustoQuery);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(query))
                {
                    // Try to translate clauses to query
                    query = QueryConditionTranslator.ToQueryString(query);
                }

                data = await this.GetIotDataQueryAsync<DeviceConnectionStatusCountModel>(
                    DeviceConnectionStateCountQueryPrefix,
                    query,
                    DeviceConnectionState,
                    null,
                    MaximumGetList); // Currently data does not show correct edge device connected status count. Will be supported in future.
            }

            return new DeviceStatisticsServiceModel(data.Result);
        }

        public async Task<List<DeviceReportServiceModel>> GetDeviceListForReport(string query)
        {
            List<DeviceReportServiceModel> devices = new List<DeviceReportServiceModel>();

            await this.GetDevices(query, null, devices);

            return devices;
        }

        public async Task<bool> LinkToGateway(string deviceId, string edgeDeviceId)
        {
            var edgeDevice = await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(edgeDeviceId);
            var leafDevice = await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(deviceId);
            leafDevice.Scope = edgeDevice.Scope;
            var updatedDevice = await this.tenantConnectionHelper.GetRegistry().UpdateDeviceAsync(leafDevice);
            return updatedDevice != null;
        }

        public async Task<BulkOperationResult> LinkDevicesToGateway(IEnumerable<string> deviceIds, string parentDeviceId, string tenantId, string userId)
        {
            if (deviceIds != null && deviceIds.Count() <= 5)
            {
                var parentDevice = await this.GetDeviceFromHub(parentDeviceId);
                List<Device> leafDevices = new List<Device>();
                foreach (string deviceId in deviceIds)
                {
                    var leafDevice = await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(deviceId);
                    leafDevices.Add(leafDevice);
                }

                bool isLinkedToOtherEdgeDevices = false;

                foreach (var leafDevice in leafDevices)
                {
                    if (!leafDevice.Capabilities.IotEdge)
                    {
                        isLinkedToOtherEdgeDevices = !string.IsNullOrWhiteSpace(leafDevice.Scope) && leafDevice.Scope != parentDevice.Scope;
                    }
                    else
                    {
                        isLinkedToOtherEdgeDevices = leafDevice.ParentScopes.Count > 0;
                    }
                }

                if (isLinkedToOtherEdgeDevices)
                {
                    return new BulkOperationResult() { IsSuccessful = false, ValidationMessages = new List<string> { "Some of the selected devices are linked to other Edge Devices, Please uncheck and try linking again" } };
                }

                bool isSuccess = true;
                foreach (var leafDevice in leafDevices)
                {
                    if (leafDevice.Capabilities.IotEdge)
                    {
                        leafDevice.ParentScopes.Add(parentDevice.Scope);
                    }
                    else
                    {
                        leafDevice.Scope = parentDevice.Scope;
                    }

                    // var result = await this.tenantConnectionHelper.GetRegistry().UpdateDevices2Async(leafDevices);
                    var result = await this.tenantConnectionHelper.GetRegistry().UpdateDeviceAsync(leafDevice);
                    isSuccess = isSuccess && result != null;
                }

                BulkOperationResult bulkResult = new BulkOperationResult();
                bulkResult.IsSuccessful = isSuccess;
                return bulkResult;
            }
            else
            {
                return await this.CreateDeviceLinkingJob(SourceCategory.Devices, parentDeviceId, string.Empty, deviceIds, tenantId, userId);
            }
        }

        public async Task<BulkOperationResult> UnlinkFromGateway(List<string> deviceIds)
        {
            List<Device> leafDevices = new List<Device>();
            foreach (string deviceId in deviceIds)
            {
                var leafDevice = await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(deviceId);
                leafDevice.Scope = null;
                leafDevices.Add(leafDevice);
            }

            bool isSuccess = true;
            foreach (var leafDevice in leafDevices)
            {
                // var result = await this.tenantConnectionHelper.GetRegistry().UpdateDevices2Async(leafDevices);
                var result = await this.tenantConnectionHelper.GetRegistry().UpdateDeviceAsync(leafDevice);
                isSuccess = isSuccess && result != null;
            }

            BulkOperationResult bulkResult = new BulkOperationResult();
            bulkResult.IsSuccessful = isSuccess;
            return bulkResult;
        }

        public async Task<BulkOperationResult> LinkDeviceGroupToGateway(string deviceGroupId, string parentDeviceId, string tenantId, string userId)
        {
            return await this.CreateDeviceLinkingJob(SourceCategory.DeviceGroup, parentDeviceId, deviceGroupId, null, tenantId, userId);
        }

        public async Task<DeviceServiceListModel> GetChildDevices(string edgeDeviceId)
        {
            var edgeDevice = await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(edgeDeviceId);
            var deviceScopeQuery = $"(deviceScope ='{edgeDevice.Scope}' OR ARRAY_CONTAINS(parentScopes,'{edgeDevice.Scope}')) AND deviceId <> '{edgeDeviceId}'";
            var data = await this.GetIotDataQueryAsync<Device>(QueryPrefix, deviceScopeQuery, null, string.Empty, 100);

            // List<string> deviceIds = new List<string>();
            // deviceIds = data.Result.Select(x => x.Id).ToList();
            // return deviceIds;
            List<DeviceServiceModel> devices = new List<DeviceServiceModel>();
            foreach (var x in data.Result)
            {
                devices.Add(new DeviceServiceModel(x));
            }

            return new DeviceServiceListModel(devices);
        }

        public async Task<DeviceLinkingJobServiceListModel> GetDeviceLinkingJobs(string tenantId)
        {
            var sql = QueryBuilder.GetDeviceDocumentsSqlByKeyLikeSearch("CollectionId", "devicelinkingjobs");
            return await this.GetDeviceLinkingJobsBasedOnSearchCriteria(sql, tenantId);
        }

        public async Task<DeviceLinkingJobServiceListModel> GetDeviceLinkingJobsByJobId(string jobId, string tenantId)
        {
            var sql = QueryBuilder.GetDeviceDocumentsSqlByKeyLikeSearch("Key", jobId);
            return await this.GetDeviceLinkingJobsBasedOnSearchCriteria(sql, tenantId);
        }

        private async Task<DeviceLinkingJobServiceListModel> GetDeviceLinkingJobsBasedOnSearchCriteria(SqlQuerySpec sql, string tenantId)
        {
            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                EnableScanInQuery = true,
            };
            List<Document> docs = await this.storageClient.QueryDocumentsAsync(
                this.DocumentDbDatabaseId,
                $"{this.DocumentDataType}-{tenantId}",
                queryOptions,
                sql,
                0,
                1000);

            return docs == null
                 ? new DeviceLinkingJobServiceListModel()
                 : new DeviceLinkingJobServiceListModel(docs
                    .Select(doc => new ValueServiceModel(doc)).Select(x => JsonConvert.DeserializeObject<DeviceLinkingJobServiceModel>(x.Data))
                    .ToList());
        }

        private async Task<Device> GetDeviceFromHub(string deviceId)
        {
            return await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(deviceId);
        }

        private async Task GetDevices(string query, string continuationToken, List<DeviceReportServiceModel> devices)
        {
            DeviceServiceListModel devicesFromQuery = null;
            devicesFromQuery = await this.GetListAsync(query, continuationToken);

            if (devicesFromQuery != null && devicesFromQuery.Items.Count() > 0)
            {
                devices.AddRange(devicesFromQuery.Items.Select(i => new DeviceReportServiceModel(i)));
                if (!string.IsNullOrWhiteSpace(devicesFromQuery.ContinuationToken))
                {
                    await this.GetDevices(query, devicesFromQuery.ContinuationToken, devices);
                }
            }
        }

        private async Task<ResultWithContinuationToken<List<Twin>>> GetTwinByQueryAsync(
            string queryPrefix,
            string query,
            string continuationToken,
            int numberOfResult)
        {
            query = string.IsNullOrEmpty(query) ? queryPrefix : $"{queryPrefix} where {query}";

            var twins = new List<Twin>();

            var twinQuery = this.tenantConnectionHelper.GetRegistry().CreateQuery(query);

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

        private async Task<Dictionary<string, Twin>> GetConnectedEdgeDevices(List<Twin> twins)
        {
            var devicesWithConnectedModules = await this.GetDevicesWithConnectedModules();
            var edgeTwins = twins
                .Where(twin => twin.Capabilities?.IotEdge ?? twin.Capabilities?.IotEdge ?? false)
                .Where(edgeDvc => devicesWithConnectedModules.Contains(edgeDvc.DeviceId))
                .ToDictionary(edgeDevice => edgeDevice.DeviceId, edgeDevice => edgeDevice);
            return edgeTwins;
        }

        private async Task<Dictionary<string, string>> GetParentDevices(List<Twin> twins)
        {
            var parentScopes = this.GetParentDeviceScopes(twins);
            string deviceListValue = string.Join(",", parentScopes.Select(p => $"'{p}'"));

            var parentTwins = this.tenantConnectionHelper.GetRegistry().CreateQuery($"SELECT * FROM Devices WHERE deviceScope IN [{deviceListValue}]");

            QueryOptions options = new QueryOptions();
            options.ContinuationToken = null;

            var deviceIdScopes = new Dictionary<string, string>();
            while (parentTwins.HasMoreResults)
            {
                var resultSet = await parentTwins.GetNextAsJsonAsync(options);
                foreach (var result in resultSet)
                {
                    var deviceTwin = JsonConvert.DeserializeObject<Device>(result);
                    if (deviceTwin.Capabilities.IotEdge)
                    {
                        deviceIdScopes.Add(deviceTwin.Scope, deviceTwin.Id);
                    }
                }
            }

            return deviceIdScopes;
        }

        private List<string> GetParentDeviceScopes(List<Twin> twins)
        {
            List<string> parentScopes = new List<string>();
            if (twins != null && twins.Count > 0)
            {
                foreach (var item in twins)
                {
                    if (item.Capabilities.IotEdge)
                    {
                        parentScopes.AddRange(item.ParentScopes);
                    }
                    else
                    {
                        parentScopes.Add(item.DeviceScope);
                    }
                }
            }

            return parentScopes;
        }

        private string GetParentDevice(Twin twin, Dictionary<string, string> parentDevices)
        {
            if (twin.Capabilities.IotEdge)
            {
                if (twin.ParentScopes.Count > 0 && parentDevices.ContainsKey(twin.ParentScopes[0]))
                {
                    return parentDevices[twin.ParentScopes[0]];
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(twin.DeviceScope))
                {
                    if (parentDevices.ContainsKey(twin.DeviceScope))
                    {
                        return parentDevices[twin.DeviceScope];
                    }
                }
            }

            return string.Empty;
        }

        private async Task<HashSet<string>> GetDevicesWithConnectedModules()
        {
            var connectedEdgeDevices = new HashSet<string>();

            var edgeModules = await this.GetModuleTwinsByQueryAsync(DevicesConnectedQuery, string.Empty);
            foreach (var model in edgeModules.Items)
            {
                connectedEdgeDevices.Add(model.DeviceId);
            }

            return connectedEdgeDevices;
        }

        private async Task<bool> DoesDeviceHaveConnectedModules(string deviceId)
        {
            var query = $"deviceId='{deviceId}' AND {DevicesConnectedQuery}";
            var edgeModules = await this.GetModuleTwinsByQueryAsync(query, string.Empty);
            return edgeModules.Items.Any();
        }

        private async Task<List<string>> GetDevicesBasedOnInputDeviceString(string deviceInput, string tenantId)
        {
            var sql = QueryBuilder.GetDeviceDocumentsSqlByKeyLikeSearch("deviceId", deviceInput);

            var twinChangeResult = await this.storageClient.QueryDocumentsAsync(
                TwinChangeDatabase,
                this.GetLifecycleCollectionId(tenantId),
                new FeedOptions
                {
                    EnableCrossPartitionQuery = true,
                },
                sql,
                0,
                10000);
            return twinChangeResult.Select(x => x.Id).ToList();
        }

        private string GetLifecycleCollectionId(string tenantId)
        {
            return this.appConfigurationClient.GetValue(
                $"{AppConfigTenantInfoKey}:{tenantId}:{AppConfigLifecycleCollectionKey}");
        }

        private async Task<ResultWithContinuationToken<List<T>>> GetIotDataQueryAsync<T>(
            string queryPrefix,
            string query,
            string groupBy,
            string continuationToken,
            int numberOfResult)
        {
            query = string.IsNullOrEmpty(query) ? queryPrefix : $"{queryPrefix} where {query}";

            query = string.IsNullOrEmpty(groupBy) ? query : $"{query} GROUP BY {groupBy}";

            var jsonResult = new List<string>();

            var jsonQuery = this.tenantConnectionHelper.GetRegistry().CreateQuery(query);

            QueryOptions options = new QueryOptions();
            options.ContinuationToken = continuationToken;

            while (jsonQuery.HasMoreResults && jsonResult.Count < numberOfResult)
            {
                var response = await jsonQuery.GetNextAsJsonAsync(options);
                options.ContinuationToken = response.ContinuationToken;
                jsonResult.AddRange(response);
            }

            List<T> result = jsonResult.Select(result => JsonConvert.DeserializeObject<T>(result)).ToList();

            return new ResultWithContinuationToken<List<T>>(result, options.ContinuationToken);
        }

        private async Task<ResultWithContinuationToken<List<T>>> GetTwinDataADXQueryAsync<T>(
                    string queryPrefix,
                    string conditionQuery,
                    string extraQuery = null)
        {
            string database = $"IoT-{this.tenantConnectionHelper.TenantId}";

            conditionQuery = string.IsNullOrEmpty(conditionQuery) ? queryPrefix : $"{queryPrefix} | where {conditionQuery}";

            conditionQuery = string.IsNullOrEmpty(extraQuery) ? conditionQuery : $"{conditionQuery} {extraQuery}";

            var results = await this.kustoQueryClient.ExecuteQueryAsync<T>(database, conditionQuery, null);

            return new ResultWithContinuationToken<List<T>>(results, null);
        }

        private async Task<BulkOperationResult> CreateDeviceLinkingJob(SourceCategory category, string parentDeviceId, string deviceGroupId, IEnumerable<string> deviceIds, string tenantId, string userId)
        {
            DeviceLinkingJobServiceModel deviceLinkingJob = new DeviceLinkingJobServiceModel()
            {
                Category = category,
                DeviceGroupId = deviceGroupId,
                DeviceIds = deviceIds,
                JobId = Guid.NewGuid().ToString(),
                ParentDeviceId = parentDeviceId,
                JobStatus = "Pending",
            };

            AuditHelper.AddAuditingData(deviceLinkingJob, userId);

            var value = JsonConvert.SerializeObject(
                                        deviceLinkingJob,
                                        Formatting.Indented,
                                        new JsonSerializerSettings
                                        {
                                            NullValueHandling = NullValueHandling.Ignore,
                                        });

            var response = await this.storageAdapterclient.UpdateAsync(string.Format(DeviceLinkingJobsCollection, parentDeviceId), deviceLinkingJob.JobId, value, null);

            try
            {
                List<EventData> events = new List<EventData>();

                DeviceLinkingRequest deviceLinkingRequest = new DeviceLinkingRequest()
                {
                    JobId = deviceLinkingJob.JobId,
                    Category = category,
                    ParentDeviceId = parentDeviceId,
                    DeviceGroupId = deviceGroupId,
                    DeviceIds = deviceIds,
                };
                var byteMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceLinkingRequest));
                var deviceLinkingJobEventData = new Azure.Messaging.EventHubs.EventData(byteMessage);
                deviceLinkingJobEventData.Properties.Add("tenant", tenantId);
                events.Add(deviceLinkingJobEventData);

                var eventHubConnString = this.config.TenantManagerService.LifecycleEventHubConnectionString;
                var eventHubName = this.config.Global.EventHub.Name;
                EventHubHelper eventHubHelper = new EventHubHelper(eventHubConnString);

                await eventHubHelper.SendMessageToEventHub(eventHubName, events.ToArray());
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to Send DeviceLinkingRequest to EventHub", e);
            }

            return new BulkOperationResult() { IsSuccessful = true, JobId = deviceLinkingJob.JobId };
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