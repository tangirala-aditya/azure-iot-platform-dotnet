// <copyright file="IoTHubMonitor.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Management.IotHub.Models;
using Microsoft.Azure.Management.Kusto;
using Microsoft.Azure.Management.Kusto.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.Azure;
using Mmm.Iot.Common.Services.External.BlobStorage;
using Mmm.Iot.Common.Services.External.KustoStorage;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.TenantManager.Services.Models;

namespace Mmm.Iot.TenantManager.Services.Tasks
{
    public class IoTHubMonitor : IHostedService, IDisposable
    {
        private const string EventHubNamespaceFormat = "eventhub-{0}";
        private const string IoTDatabaseNameFormat = "IoT-{0}";
        private readonly CancellationTokenSource stoppingCts = new CancellationTokenSource();
        private readonly List<ADXDatabaseModel> tenantADXDatabaseList = new List<ADXDatabaseModel>
        {
            new ADXDatabaseModel()
            {
                DatabaseNameFormat = IoTDatabaseNameFormat,
                SoftDeletePeriod = null,
                HotCachePeriod = new TimeSpan(0, 0, 0, 0),
            },
        };

        private Task executingTask;
        private ITableStorageClient tableStorageClient;
        private IBlobStorageClient blobStorageClient;
        private IAzureManagementClient azureManagementClient;
        private IAppConfigurationClient appConfigurationClient;
        private AppConfig config;
        private IKustoTableManagementClient kustoTableManagementClient;

        public IoTHubMonitor(ITableStorageClient tableStorageClient, IBlobStorageClient blobStorageClient, IAzureManagementClient azureManagementClient, IAppConfigurationClient appConfigurationClient, AppConfig config, IKustoTableManagementClient kustoTableManagementClient)
        {
            this.tableStorageClient = tableStorageClient;
            this.blobStorageClient = blobStorageClient;
            this.azureManagementClient = azureManagementClient;
            this.appConfigurationClient = appConfigurationClient;
            this.config = config;
            this.kustoTableManagementClient = kustoTableManagementClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Store the task we're executing
            if (this.executingTask == null)
            {
                this.executingTask = this.ExecuteAsync(this.stoppingCts.Token);
            }

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (this.executingTask.IsCompleted)
            {
                return this.executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "I added a timeout value")]
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (this.executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                this.stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(this.executingTask, Task.Delay(5000, cancellationToken));
            }
        }

        public virtual void Dispose()
        {
            this.stoppingCts.Cancel();
            this.stoppingCts.Dispose();
        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("Getting Items...");
                    TableQuery<TenantModel> query = new TableQuery<TenantModel>();
                    query.Where(TableQuery.CombineFilters(
                        TableQuery.GenerateFilterConditionForBool("IsIotHubDeployed", QueryComparisons.Equal, false),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, DateTime.Now.AddHours(-1))));

                    var items = await this.tableStorageClient.QueryAsync("tenant", query, stoppingToken);
                    foreach (var item in items)
                    {
                        Console.WriteLine($"Processing {item.TenantId}");
                        try
                        {
                            await this.blobStorageClient.CreateBlobContainerIfNotExistsAsync(item.TenantId + "-iot-file-upload");
                            Console.WriteLine("File Upload Container Made");
                            IotHubDescription iothub = await this.azureManagementClient.IotHubManagementClient.RetrieveAsync(item.IotHubName, stoppingToken);

                            if (iothub.Properties.State == "Active")
                            {
                                Console.WriteLine("IoT Hub found");
                                var connectionString = this.azureManagementClient.IotHubManagementClient.GetConnectionString(iothub.Name);
                                await this.appConfigurationClient.SetValueAsync($"tenant:{item.TenantId}:iotHubConnectionString", connectionString);
                                Assembly assembly = Assembly.GetExecutingAssembly();
                                StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("dps.json"));
                                string template = await reader.ReadToEndAsync();
                                template = string.Format(
                                    template,
                                    item.DpsName,
                                    this.config.Global.Location,
                                    connectionString);
                                await this.azureManagementClient.DeployTemplateAsync(template);

                                item.IsIotHubDeployed = true;
                                await this.tableStorageClient.InsertOrReplaceAsync<TenantModel>("tenant", item);

                                if (string.Equals(this.config.DeviceTelemetryService.Messages.TelemetryStorageType, TelemetryStorageTypeConstants.Ade, StringComparison.OrdinalIgnoreCase))
                                {
                                    string eventHubNameSpace = await this.SetupEventHub(item.TenantId);
                                    await this.SetupADXDatabase(item.TenantId);

                                    await this.ADXTelemetrySetup(item.TenantId, string.Format(IoTDatabaseNameFormat, item.TenantId), eventHubNameSpace);
                                    await this.ADXDeviceTwinSetup(item.TenantId, string.Format(IoTDatabaseNameFormat, item.TenantId), eventHubNameSpace);
                                }
                            }
                        }
                        catch (Microsoft.Azure.Management.IotHub.Models.ErrorDetailsException e)
                        {
                            if (e.Message == "Operation returned an invalid status code 'NotFound'")
                            {
                                Console.WriteLine("This is where we deploy IoT Hub");
                                Assembly assembly = Assembly.GetExecutingAssembly();
                                StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("iothub.json"));
                                string template = await reader.ReadToEndAsync();
                                template = string.Format(
                                    template,
                                    item.IotHubName,
                                    this.config.Global.Location,
                                    this.config.Global.SubscriptionId,
                                    this.config.Global.ResourceGroup,
                                    item.TenantId,
                                    "$twin.properties.desired.batchedTelemetry",
                                    this.config.TenantManagerService.TelemetryEventHubConnectionString,
                                    this.config.TenantManagerService.TwinChangeEventHubConnectionString,
                                    this.config.TenantManagerService.LifecycleEventHubConnectionString,
                                    this.config.TenantManagerService.DeviceTwinMirrorEventHubConnectionString,
                                    this.config.Global.StorageAccountConnectionString);
                                await this.azureManagementClient.DeployTemplateAsync(template);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
                finally
                {
                    await Task.Delay(15000, stoppingToken);
                }
            }
        }

        private async Task<string> SetupEventHub(string tenantId)
        {
            string eventHubNameSpace = string.Format(EventHubNamespaceFormat, tenantId.Substring(0, 8));
            await this.azureManagementClient.EventHubsManagementClient.CreateNamespace(eventHubNameSpace);

            string nameSpaceConnString = await this.azureManagementClient.EventHubsManagementClient.GetPrimaryConnectionString(eventHubNameSpace);

            await this.appConfigurationClient.SetValueAsync($"tenant:{tenantId}:eventHubConn", nameSpaceConnString);

            this.azureManagementClient.EventHubsManagementClient.CreateEventHub(eventHubNameSpace, $"{tenantId}-telemetry");

            this.azureManagementClient.EventHubsManagementClient.CreateEventHub(eventHubNameSpace, $"{tenantId}-devicetwin");

            return eventHubNameSpace;
        }

        private async Task SetupADXDatabase(string tenantId)
        {
            foreach (var item in this.tenantADXDatabaseList)
            {
                var databaseName = string.Format(item.DatabaseNameFormat, tenantId);

                Console.WriteLine($"Creating a {databaseName} DB in Data Explorer");

                await this.azureManagementClient.KustoClusterManagementClient.CreateDBInClusterAsync(databaseName, item.SoftDeletePeriod, item.HotCachePeriod);

                Console.WriteLine($"Created a {databaseName} DB in Data Explorer");
            }
        }

        private async Task ADXTelemetrySetup(string tenantId, string databaseName, string eventHubNameSpace)
        {
            Console.WriteLine($"Creating telemetry table and mapping in {tenantId} DB in Data Explorer");

            var tableName = "Telemetry";
            var tableMappingName = $"TelemetryEvents_JSON_Mapping-{tenantId}";
            var tableSchema = new[]
            {
                  Tuple.Create("DeviceId", "System.String"),
                  Tuple.Create("Data", "System.Object"),
                  Tuple.Create("TimeStamp", "System.Datetime"),
            };
            var mappingSchema = new ColumnMapping[]
            {
                  new ColumnMapping() { ColumnName = "DeviceId", ColumnType = "string", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.deviceId" } } },
                  new ColumnMapping() { ColumnName = "Data", ColumnType = "dynamic", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.data" } } },
                  new ColumnMapping() { ColumnName = "TimeStamp", ColumnType = "datetime", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.dateTimeReceived" } } },
            };

            string dataConnectionName = $"TelemetryDataConnect-{tenantId.Substring(0, 8)}";
            string eventHubName = $"{tenantId}-telemetry";

            await this.ADXTableSetup(tenantId, databaseName, tableName, tableSchema, tableMappingName, mappingSchema, dataConnectionName, eventHubNameSpace, eventHubName);

            this.kustoTableManagementClient.AlterTableRetentionPolicy(tableName, databaseName, new TimeSpan(30, 0, 0, 0), DataRecoverability.Disabled);
        }

        private async Task ADXDeviceTwinSetup(string tenantId, string databaseName, string eventHubNameSpace)
        {
            Console.WriteLine($"Creating telemetry table and mapping in {tenantId} DB in Data Explorer");

            var tableName = "DeviceTwin";
            var tableMappingName = $"DeviceTwinEvents_JSON_Mapping-{tenantId}";
            var tableSchema = new[]
            {
                  Tuple.Create("DeviceId", "System.String"),
                  Tuple.Create("Twin", "System.Object"),
                  Tuple.Create("TimeStamp", "System.Datetime"),
                  Tuple.Create("DeviceCreatedDate", "System.Datetime"),
                  Tuple.Create("IsDeleted", "System.Boolean"),
            };
            var mappingSchema = new ColumnMapping[]
            {
                  new ColumnMapping() { ColumnName = "DeviceId", ColumnType = "string", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.deviceId" } } },
                  new ColumnMapping() { ColumnName = "Twin", ColumnType = "dynamic", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.data" } } },
                  new ColumnMapping() { ColumnName = "TimeStamp", ColumnType = "datetime", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.timeStamp" } } },
                  new ColumnMapping() { ColumnName = "DeviceCreatedDate", ColumnType = "datetime", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.deviceCreatedDate" } } },
                  new ColumnMapping() { ColumnName = "IsDeleted", ColumnType = "bool", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.isDeleted" } } },
            };

            string dataConnectionName = $"DeviceTwinDataConnect-{tenantId.Substring(0, 8)}";
            string eventHubName = $"{tenantId}-devicetwin";

            await this.ADXTableSetup(tenantId, databaseName, tableName, tableSchema, tableMappingName, mappingSchema, dataConnectionName, eventHubNameSpace, eventHubName);
        }

        private async Task ADXTableSetup(
            string tenantId,
            string databaseName,
            string tableName,
            Tuple<string, string>[] tableSchema,
            string tableMappingName,
            ColumnMapping[] mappingSchema,
            string dataConnectionName,
            string eventHubNameSpace,
            string eventHubName)
        {
            Console.WriteLine($"Creating telemetry table and mapping in {tenantId} DB in Data Explorer");

            this.kustoTableManagementClient.CreateTable(tableName, tableSchema, databaseName);

            this.kustoTableManagementClient.CreateTableMapping(tableMappingName, mappingSchema, tableName, databaseName);

            this.kustoTableManagementClient.EnableStreamingIngestionPolicyToTable(tableName, databaseName);

            string consumerGroup = "$Default";

            await this.azureManagementClient.KustoClusterManagementClient.AddEventHubDataConnectionAsync(dataConnectionName, databaseName, tableName, tableMappingName, eventHubNameSpace, eventHubName, consumerGroup);
        }

        private class ADXDatabaseModel
        {
            public string DatabaseNameFormat { get; set; }

            public TimeSpan? SoftDeletePeriod { get; set; }

            public TimeSpan HotCachePeriod { get; set; }
        }
    }
}