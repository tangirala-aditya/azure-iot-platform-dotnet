// <copyright file="KustoClusterManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.Azure.Management.Kusto;
using Microsoft.Azure.Management.Kusto.Models;
using Microsoft.Rest.Azure;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public class KustoClusterManagementClient : IKustoClusterManagementClient
    {
        private AppConfig config;
        private KustoManagementClient client;

        public KustoClusterManagementClient(KustoManagementClient client, AppConfig config)
        {
            this.client = client;
            this.config = config;
            this.client.SubscriptionId = this.config.Global.SubscriptionId;
        }

        public async Task<StatusResultServiceModel> StatusAsync()
        {
            try
            {
                var result = this.client.IsDefined();
                await Task.CompletedTask; // Just to keep the signature async, later this should be replaced with more robust status check

                // If the call above does not fail then return a healthy status
                return new StatusResultServiceModel(result, result ? "Alive and well!" : "Undefined KustoClusterManagementClient");
            }
            catch (Exception e)
            {
                return new StatusResultServiceModel(false, $"Kusto Cluster status check failed: {e.Message}");
            }
        }

        public async Task CreateDBInClusterAsync(string databaseName, TimeSpan? softDeletePeriod, TimeSpan? hotCachePeriod = null)
        {
            try
            {
                var database = new ReadWriteDatabase(
                    location: this.config.Global.Location,
                    softDeletePeriod: softDeletePeriod,
                    hotCachePeriod: hotCachePeriod);

                await this.client.Databases.CreateOrUpdateAsync(
                    this.config.Global.ResourceGroup,
                    this.config.Global.DataExplorer.Name,
                    databaseName,
                    database);
            }
            catch (CloudException e)
            {
                if (e.Body.Code == "ResourceNotFound")
                {
                    throw new ResourceNotFoundException($"Error Creating kusto database {databaseName}");
                }

                throw e;
            }
        }

        public async Task AddEventHubDataConnectionAsync(string dataConnectName, string databaseName, string tableName, string tableMappingName, string eventHubNamespace, string eventHubName, string eventHubConsumerGroup)
        {
            try
            {
                await this.CreateOrUpdateDataConnection(
                        databaseName,
                        dataConnectName,
                        new EventHubDataConnection(
                            $"/subscriptions/{this.config.Global.SubscriptionId}/resourceGroups/{this.config.Global.ResourceGroup}/providers/Microsoft.EventHub/namespaces/{eventHubNamespace}/eventhubs/{eventHubName}",
                            eventHubConsumerGroup,
                            location: this.config.Global.Location,
                            tableName: tableName,
                            mappingRuleName: tableMappingName,
                            dataFormat: "JSON",
                            compression: "None"));
            }
            catch (CloudException e)
            {
                if (e.Body.Code == "ResourceNotFound")
                {
                    throw new ResourceNotFoundException($"Error Creating EventHub Data Connection from kusto database {databaseName}, IotHub {eventHubName}");
                }

                throw e;
            }
        }

        public async Task AddIoTHubDataConnectionAsync(string dataConnectName, string databaseName, string tableName, string tableMappingName, string iotHubName, string iotHubConsumerGroup)
        {
            try
            {
                await this.CreateOrUpdateDataConnection(
                        databaseName,
                        dataConnectName,
                        new IotHubDataConnection(
                            $"/subscriptions/{this.config.Global.SubscriptionId}/resourceGroups/{this.config.Global.ResourceGroup}/providers/Microsoft.Devices/IotHubs/{iotHubName}",
                            iotHubConsumerGroup,
                            sharedAccessPolicyName: "iothubowner",
                            location: this.config.Global.Location,
                            tableName: tableName,
                            mappingRuleName: tableMappingName,
                            dataFormat: "JSON",
                            eventSystemProperties: new List<string>()
                            {
                            "iothub-connection-device-id",
                            "iothub-enqueuedtime",
                            }));
            }
            catch (CloudException e)
            {
                if (e.Body.Code == "ResourceNotFound")
                {
                    throw new ResourceNotFoundException($"Error Creating IoTHub Data Connection from kusto database {databaseName}, IotHub {iotHubName}");
                }

                throw e;
            }
        }

        public async Task DeleteDatabaseAsync(string databaseName)
        {
            ReadWriteDatabase result = null;

            try
            {
                result = await this.client.Databases.GetAsync(
                                    this.config.Global.ResourceGroup,
                                    this.config.Global.DataExplorer.Name,
                                    databaseName) as ReadWriteDatabase;

                if (result != null && result.Name.Contains(databaseName, StringComparison.OrdinalIgnoreCase))
                {
                    await this.client.Databases.DeleteAsync(
                                        this.config.Global.ResourceGroup,
                                        this.config.Global.DataExplorer.Name,
                                        databaseName);
                }
            }
            catch (CloudException e)
            {
                if (e.Body.Code == "ResourceNotFound")
                {
                    throw new ResourceNotFoundException($"Unable to Delete kusto database {databaseName}");
                }

                throw e;
            }
        }

        private async Task CreateOrUpdateDataConnection(string databaseName, string dataConnectName, DataConnection dataConnection)
        {
            await this.client.DataConnections.CreateOrUpdateAsync(
                        this.config.Global.ResourceGroup,
                        this.config.Global.DataExplorer.Name,
                        databaseName,
                        dataConnectName,
                        dataConnection);
        }
    }
}