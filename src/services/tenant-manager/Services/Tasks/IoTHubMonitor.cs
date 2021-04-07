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
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.TenantManager.Services.Models;

namespace Mmm.Iot.TenantManager.Services.Tasks
{
    public class IoTHubMonitor : IHostedService, IDisposable
    {
        private readonly CancellationTokenSource stoppingCts = new CancellationTokenSource();
        private Task executingTask;
        private ITableStorageClient tableStorageClient;
        private IBlobStorageClient blobStorageClient;
        private IAzureManagementClient azureManagementClient;
        private IAppConfigurationClient appConfigurationClient;
        private AppConfig config;

        public IoTHubMonitor(ITableStorageClient tableStorageClient, IBlobStorageClient blobStorageClient, IAzureManagementClient azureManagementClient, IAppConfigurationClient appConfigurationClient, AppConfig config)
        {
            this.tableStorageClient = tableStorageClient;
            this.blobStorageClient = blobStorageClient;
            this.azureManagementClient = azureManagementClient;
            this.appConfigurationClient = appConfigurationClient;
            this.config = config;
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

                                Console.WriteLine("Creating a DB in Data Explorer");

                                var authenticationContext = new AuthenticationContext($"https://login.windows.net/{this.config.Global.AzureActiveDirectory.TenantId}");
                                var credential = new ClientCredential(
                                    this.config.Global.AzureActiveDirectory.AppId,
                                    this.config.Global.AzureActiveDirectory.AppSecret);

                                var result = await authenticationContext.AcquireTokenAsync(resource: "https://management.core.windows.net/", clientCredential: credential);

                                var credentials = new TokenCredentials(result.AccessToken, result.AccessTokenType);

                                var kustoManagementClient = new KustoManagementClient(credentials)
                                {
                                    SubscriptionId = this.config.Global.SubscriptionId,
                                };

                                var hotCachePeriod = new TimeSpan(0, 0, 0, 0);
                                var softDeletePeriod = new TimeSpan(60, 0, 0, 0);
                                var databaseName = item.TenantId;
                                var database = new ReadWriteDatabase(
                                    location: this.config.Global.Location,
                                    softDeletePeriod: softDeletePeriod,
                                    hotCachePeriod: hotCachePeriod);

                                await kustoManagementClient.Databases.CreateOrUpdateAsync(
                                    this.config.Global.ResourceGroup,
                                    this.config.Global.DataExplorer.Name,
                                    databaseName,
                                    database);

                                _ = kustoManagementClient.Databases.Get(this.config.Global.ResourceGroup, this.config.Global.DataExplorer.Name, databaseName) as ReadWriteDatabase;

                                Console.WriteLine($"Created a {item.TenantId} DB in Data Explorer");

                                Console.WriteLine($"Creating telemetry table and mapping in {item.TenantId} DB in Data Explorer");

                                var kustoUri = $"https://{this.config.Global.DataExplorer.Name}.{this.config.Global.Location}.kusto.windows.net/";

                                var kustoConnectionStringBuilder = new KustoConnectionStringBuilder(kustoUri).WithAadUserPromptAuthentication(this.config.Global.AzureActiveDirectory.TenantId);

                                var tableName = "telemetry";
                                var tableMappingName = "Events_CSV_Mapping";
                                var tableSchema = new[]
                                {
                                    Tuple.Create("deviceId", "System.String"),
                                    Tuple.Create("telemetry", "System.Object"),
                                };
                                var mappingSchema = new ColumnMapping[]
                                {
                                    new ColumnMapping() { ColumnName = "deviceId", ColumnType = "string", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.iothub-connection-device-id" } } },
                                    new ColumnMapping() { ColumnName = "telemetry", ColumnType = "dynamic", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$" } } },
                                };

                                using (var kustoClient = KustoClientFactory.CreateCslAdminProvider(kustoConnectionStringBuilder))
                                {
                                    var command =
                                        CslCommandGenerator.GenerateTableCreateCommand(
                                            tableName,
                                            tableSchema);

                                    kustoClient.ExecuteControlCommand(databaseName, command);

                                    command = CslCommandGenerator.GenerateTableMappingCreateCommand(
                                        Kusto.Data.Ingestion.IngestionMappingKind.Json,
                                        tableName,
                                        tableMappingName,
                                        mappingSchema);

                                    kustoClient.ExecuteControlCommand(databaseName, command);
                                }

                                await kustoManagementClient.DataConnections.CreateOrUpdateAsync(
                                    this.config.Global.ResourceGroup,
                                    this.config.Global.DataExplorer.Name,
                                    databaseName,
                                    "telemetryDataConnect", // Add a tenentid to connection name
                                    new EventHubDataConnection(
                                        $"/subscriptions/{this.config.Global.SubscriptionId}/resourceGroups/{this.config.Global.ResourceGroup}/providers/Microsoft.EventHub/namespaces/{this.config.Global.EventHub.Name}/eventhubs/telemetry",
                                        "$Default",
                                        location: this.config.Global.Location,
                                        tableName: tableName,
                                        mappingRuleName: tableMappingName,
                                        dataFormat: "JSON",
                                        compression: "None"));
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
    }
}