// <copyright file="DeviceGroupMigration.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using MigrateIoTDeviceTwinToADX.Helpers;
using MigrateIoTDeviceTwinToADX.Models;
using Mmm.Iot.MigrateIoTDeviceTwinToADX.Config;
using Mmm.Iot.MigrateIoTDeviceTwinToADX.Helpers;
using Mmm.Iot.MigrateIoTDeviceTwinToADX.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.MigrateIoTDeviceTwinToADX
{
    public class DeviceGroupMigration
    {
        private const string DeviceGroupId = "DeviceGroupId";
        private const string DeviceGroupName = "DeviceGroupName";
        private const string DeviceGroupConditions = "DeviceGroupConditions";
        private const string TimeStamp = "TimeStamp";
        private const string IsDeleted = "IsDeleted";
        private readonly TableStorageHelper tableStorageClient;
        private readonly TenantConnectionHelper tenantConnectionHelper;
        private readonly ILogger logger;
        private readonly AppConfig appConfig;

        public DeviceGroupMigration(TableStorageHelper tableStorageClient, TenantConnectionHelper tenantConnectionHelper, ILogger logger, AppConfig appConfig)
        {
            this.tableStorageClient = tableStorageClient;
            this.tenantConnectionHelper = tenantConnectionHelper;
            this.logger = logger;
            this.appConfig = appConfig;
        }

        public string TenantTableId => "tenant";

        private FeedOptions DefaultQueryOptions => new FeedOptions
        {
            EnableCrossPartitionQuery = true,
            EnableScanInQuery = true,
        };

        public async Task Start()
        {
            List<TenantModel> tenantList = await this.GetAllActiveTenantAsync();
            var tasks = tenantList.Select(async tenant =>
            {
                string tenantId = tenant.TenantId;

                try
                {
                    this.logger.LogInformation($"Started device group migration of tenant: {tenantId} ");

                    IEnumerable<DeviceGroup> deviceGroupList = await this.GetDeviceGroups(tenantId);
                    var connectionString = this.tenantConnectionHelper.GetEventHubConnectionString(Convert.ToString(tenantId));
                    EventHubHelper eventHubHelper = new EventHubHelper(connectionString);
                    List<EventData> events = new List<EventData>();
                    foreach (var deviceGroup in deviceGroupList)
                    {
                        var result = this.GetDeviceGroupsForADX(deviceGroup);

                        if (result != null)
                        {
                            events.Add(result);
                        }
                    }

                    await eventHubHelper.SendMessageToEventHub($"{tenantId}-devicegroup", events);

                    this.logger.LogInformation($"Completed device twin migration of tenant: {tenantId} ");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Failed device twin migration of tenant: {tenantId} ");
                }
            });

            await Task.WhenAll(tasks);
        }

        public virtual async Task<List<TenantModel>> GetAllActiveTenantAsync()
        {
            try
            {
                // Load the tenant from table storage
                TableQuery<TenantModel> query = new TableQuery<TenantModel>().Where(TableQuery.GenerateFilterConditionForBool("IsIotHubDeployed", QueryComparisons.Equal, true));
                List<TenantModel> result = await this.tableStorageClient.QueryAsync<TenantModel>(this.TenantTableId, query);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to retrieve the active tenants from table storage", e);
            }
        }

        public async Task<IEnumerable<DeviceGroup>> GetDeviceGroups(string tenantId)
        {
            var sqlQuery = CosmosOperations.GetDocumentsByCollectionId("CollectionId", "devicegroups");
            string cosmosConnectionString = this.appConfig.Global.CosmosDb.DocumentDbConnectionString;
            CosmosOperations storageClient = await CosmosOperations.GetClientAsync(cosmosConnectionString);
            var docs = await storageClient.QueryDocumentsAsync(
               "pcs-storage",
               $"pcs-{tenantId}",
               this.DefaultQueryOptions,
               sqlQuery,
               0,
               1000);

            var result = docs.Select(doc => this.CreateDeviceGroupModel(doc));

            return result;
        }

        private DeviceGroup CreateDeviceGroupModel(Document resource)
        {
            var key = resource.GetPropertyValue<string>("Key");
            var data = resource.GetPropertyValue<string>("Data");

            var output = JsonConvert.DeserializeObject<DeviceGroup>(data);
            output.Id = key;
            return output;
        }

        private EventData GetDeviceGroupsForADX(DeviceGroup deviceGroup)
        {
            JObject deviceGroupDeviceMappingJson = new JObject();
            deviceGroupDeviceMappingJson.Add(DeviceGroupId, deviceGroup.Id);
            deviceGroupDeviceMappingJson.Add(DeviceGroupName, deviceGroup.DisplayName);
            deviceGroupDeviceMappingJson.Add(DeviceGroupConditions, QueryConditionTranslator.ToADXQueryString(JsonConvert.SerializeObject(deviceGroup.Conditions)));
            deviceGroupDeviceMappingJson.Add(TimeStamp, DateTime.UtcNow);
            deviceGroupDeviceMappingJson.Add(IsDeleted, false);
            var byteMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceGroupDeviceMappingJson));
            var deviceMappingEventData = new EventData(byteMessage);
            return deviceMappingEventData;
        }
    }
}