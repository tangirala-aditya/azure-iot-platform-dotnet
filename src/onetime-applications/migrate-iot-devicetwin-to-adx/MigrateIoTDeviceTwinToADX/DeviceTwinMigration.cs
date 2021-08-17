// <copyright file="DeviceTwinMigration.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Mmm.Iot.MigrateIoTDeviceTwinToADX.Helpers;
using Mmm.Iot.MigrateIoTDeviceTwinToADX.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.MigrateIoTDeviceTwinToADX
{
    public class DeviceTwinMigration
    {
        private const string DeviceId = "deviceId";
        private const string TimeStamp = "timeStamp";
        private const string TimeReceived = "_timeReceived";
        private const string EventOpType = "eventOperationType";
        private const string IsDeleted = "isDeleted";
        private const string DeviceCreatedDate = "deviceCreatedDate";
        private const string Data = "data";
        private readonly TableStorageHelper tableStorageClient;
        private readonly TenantConnectionHelper tenantConnectionHelper;
        private readonly ILogger logger;

        public DeviceTwinMigration(TableStorageHelper tableStorageClient, TenantConnectionHelper tenantConnectionHelper, ILogger logger)
        {
            this.tableStorageClient = tableStorageClient;
            this.tenantConnectionHelper = tenantConnectionHelper;
            this.logger = logger;
        }

        public string TenantTableId => "tenant";

        public async Task Start()
        {
            List<TenantModel> tenantList = await this.GetAllActiveTenantAsync();
            var tasks = tenantList.Select(async tenant =>
            {
                string tenantId = tenant.TenantId;

                try
                {
                    this.logger.LogInformation($"Started device twin migration of tenant: {tenantId} ");

                    List<Twin> deviceTwinList = await this.GetDevices(tenantId);
                    var connectionString = this.tenantConnectionHelper.GetEventHubConnectionString(Convert.ToString(tenantId));
                    EventHubHelper eventHubHelper = new EventHubHelper(connectionString);
                    var eventDatas = new ConcurrentBag<EventData>();
                    Parallel.ForEach(deviceTwinList, deviceTwin =>
                    {
                        JObject deviceTwinJson = new JObject();
                        deviceTwinJson.Add(DeviceId, deviceTwin.DeviceId);
                        deviceTwinJson.Add(TimeStamp, DateTime.UtcNow);
                        deviceTwinJson.Add(TimeReceived, DateTime.UnixEpoch);
                        deviceTwinJson.Add(EventOpType, "updateTwin");
                        deviceTwinJson.Add(IsDeleted, false);
                        deviceTwinJson.Add(Data, deviceTwin.ToJson());
                        deviceTwinJson.Add(DeviceCreatedDate, default(DateTime));

                        var byteMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceTwinJson));
                        var eventDeviceTwinData = new Azure.Messaging.EventHubs.EventData(byteMessage);
                        eventDeviceTwinData.Properties.Add(DeviceId, deviceTwin.DeviceId);
                        eventDatas.Add(eventDeviceTwinData);
                    });

                    await eventHubHelper.SendMessageToEventHub($"{tenantId}-devicetwin", eventDatas.ToList());

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

        public virtual async Task<List<Twin>> GetDevices(string tenant)
        {
            try
            {
                string query = "SELECT * FROM devices";

                var twins = new List<Twin>();
                var twinQuery = this.tenantConnectionHelper.GetRegistry(Convert.ToString(tenant)).CreateQuery(query);

                QueryOptions options = new QueryOptions();

                while (twinQuery.HasMoreResults)
                {
                    var response = await twinQuery.GetNextAsTwinAsync(options);
                    options.ContinuationToken = response.ContinuationToken;
                    twins.AddRange(response);
                }

                return twins;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to retrieve the active tenants from table storage", e);
            }
        }
    }
}