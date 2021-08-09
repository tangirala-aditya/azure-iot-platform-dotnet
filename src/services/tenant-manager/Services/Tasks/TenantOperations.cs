// <copyright file="TenantOperations.cs" company="3M">
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
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Management.IotHub.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.WindowsAzure.Storage.Blob;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.Azure;
using Mmm.Iot.Common.Services.External.BlobStorage;
using Mmm.Iot.Common.Services.External.KustoStorage;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.TenantManager.Services.External;
using Mmm.Iot.TenantManager.Services.Models;

namespace Mmm.Iot.TenantManager.Services.Tasks
{
    public class TenantOperations : IHostedService, IDisposable
    {
        private const string TableName = "tenantOperations";
        private const string EventHubNamespaceFormat = "eventhub-{0}";
        private const string IoTDatabaseNameFormat = "IoT-{0}";
        private readonly CancellationTokenSource stoppingCts = new CancellationTokenSource();
        private Task executingTask;
        private ITableStorageClient tableStorageClient;
        private IBlobStorageClient blobStorageClient;
        private IAzureManagementClient azureManagementClient;
        private IAppConfigurationClient appConfigurationClient;
        private AppConfig config;
        private IGrafanaClient grafanaClient;
        private IKustoTableManagementClient kustoTableManagementClient;

        public TenantOperations(ITableStorageClient tableStorageClient, IBlobStorageClient blobStorageClient, IAzureManagementClient azureManagementClient, IAppConfigurationClient appConfigurationClient, AppConfig config, IGrafanaClient grafanaClient, IKustoTableManagementClient kustoTableManagementClient)
        {
            this.tableStorageClient = tableStorageClient;
            this.blobStorageClient = blobStorageClient;
            this.azureManagementClient = azureManagementClient;
            this.appConfigurationClient = appConfigurationClient;
            this.config = config;
            this.grafanaClient = grafanaClient;
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
                Console.WriteLine("Getting Tenant Operations...");
                TableQuery<TenantOperationModel> query = new TableQuery<TenantOperationModel>();

                var items = await this.tableStorageClient.QueryAsync(TableName, query, stoppingToken);
                foreach (var item in items)
                {
                    try
                    {
                        if (item.Type == TenantOperation.IoTHubDeletion)
                        {
                            Console.WriteLine($"Processing {item.TenantId}");
                            try
                            {
                                Console.WriteLine($"Deleting {item.Name}...");

                                await this.azureManagementClient.IotHubManagementClient.DeleteAsync(
                                    item.Name,
                                    stoppingToken);
                                Console.WriteLine($"Deleting Table Operation Record...");
                                await this.tableStorageClient.DeleteAsync(TableName, item);
                            }
                            catch (Microsoft.Azure.Management.IotHub.Models.ErrorDetailsException e)
                            {
                                if (e.Message == "Operation returned an invalid status code 'NotFound'")
                                {
                                    // Handle edge case
                                }
                            }
                        }

                        if (item.Type == TenantOperation.SaJobDeletion)
                        {
                            try
                            {
                                var job = await this.azureManagementClient.AsaManagementClient.RetrieveAsync(
                                    item.Name,
                                    null,
                                    stoppingToken);
                                Console.WriteLine($"SA job {item.Name} found");
                                if (new List<string> { "Starting", "Running" }.Contains(job.JobState))
                                {
                                    Console.WriteLine($"Stopping job");
                                    await this.azureManagementClient.AsaManagementClient.StopAsync(
                                        item.Name,
                                        null,
                                        stoppingToken);
                                }
                                else if (job.JobState != "Stopping")
                                {
                                    Console.WriteLine($"Deleting job");
                                    await this.azureManagementClient.AsaManagementClient.DeleteAsync(
                                        item.Name,
                                        null,
                                        stoppingToken);
                                }
                            }
                            catch (ResourceNotFoundException)
                            {
                                // Item does not exist... delete the record
                                Console.WriteLine($"SA job {item.Name} does not exist...deleting tenant operation");
                                await this.tableStorageClient.DeleteAsync(TableName, item);
                            }
                        }

                        if (item.Type == TenantOperation.SaJobCreation)
                        {
                            await this.blobStorageClient.CreateBlobContainerIfNotExistsAsync(item.TenantId);
                            Console.WriteLine("File Upload Container Made");
                            var tenant = await this.tableStorageClient.RetrieveAsync<TenantModel>(
                                "tenant",
                                item.TenantId.Substring(0, 1),
                                item.TenantId);

                            this.azureManagementClient.IotHubManagementClient.AddConsumerGroup(
                                tenant.IotHubName,
                                "events",
                                "sajobconsumergroup");
                            Console.WriteLine("Event Consumer Group created");
                            try
                            {
                                var job = await this.azureManagementClient.AsaManagementClient.RetrieveAsync(
                                    item.Name,
                                    null,
                                    stoppingToken);
                                Console.WriteLine($"SA job {item.Name} found");
                                Console.WriteLine($"Updating tenant table...");
                                tenant.SAJobName = item.Name;
                                await this.tableStorageClient.InsertOrMergeAsync("tenant", tenant);

                                string eventHubNameSpace = await this.SetupEventHub(item.TenantId);
                                var databaseName = string.Format(IoTDatabaseNameFormat, item.TenantId);
                                await this.azureManagementClient.KustoClusterManagementClient.CreateDatabaseIfNotExistAsync(databaseName);

                                await this.ADXAlertsSetup(item.TenantId, databaseName, eventHubNameSpace);

                                Console.WriteLine($"Deleting tenant operations table...");
                                await this.tableStorageClient.DeleteAsync(TableName, item);
                            }
                            catch (ResourceNotFoundException)
                            {
                                // Item does not exist... delete the record
                                Console.WriteLine($"SA job {item.Name} does not exist...creating it");
                                Assembly assembly = Assembly.GetExecutingAssembly();
                                StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("sajob.json"));
                                string template = await reader.ReadToEndAsync();
                                template = string.Format(
                                    template,
                                    item.Name,
                                    this.config.Global.Location,
                                    this.config.Global.StorageAccount.Name,
                                    this.config.Global.StorageAccountConnectionString.Split(";")[2].Replace("AccountKey=", string.Empty),
                                    tenant.IotHubName,
                                    this.azureManagementClient.IotHubManagementClient.GetAccessKey(
                                        tenant.IotHubName,
                                        "iothubowner"),
                                    item.TenantId,
                                    this.config.Global.EventHub.Name,
                                    this.config.Global.EventHub.RootKey,
                                    this.config.Global.CosmosDb.AccountName,
                                    this.config.Global.CosmosDb.DocumentDbAuthKey);
                                await this.azureManagementClient.DeployTemplateAsync(template);
                            }
                        }

                        if (item.Type == TenantOperation.DpsDeletion)
                        {
                            try
                            {
                                Console.WriteLine($"Deleting {item.Name}...");
                                await this.azureManagementClient.DpsManagmentClient.DeleteAsync(item.Name);
                            }
                            catch (ResourceNotFoundException)
                            {
                                Console.WriteLine($"Deleting Table Operation Record...");
                                await this.tableStorageClient.DeleteAsync(TableName, item);
                            }
                        }

                        if (item.Type == TenantOperation.GrafanaDashboardCreation)
                        {
                            await this.grafanaClient.CreateAPIKeyIsNotFound();

                            string tenantIdSubstring = item.TenantId.Substring(0, 8);
                            string mainDashboardName = $"Main:{tenantIdSubstring}";
                            string mainDashboardUid = tenantIdSubstring;
                            string mainDashboardSubUrl = $"main-{tenantIdSubstring}";

                            string tenantIdLastSubstring = item.TenantId.Substring(item.TenantId.Length - 8, 8);
                            string adminDashboardName = $"Admin:{tenantIdLastSubstring}";
                            string adminDashboardUid = tenantIdLastSubstring;
                            string adminDashboardSubUrl = $"admin-{tenantIdLastSubstring}";

                            var tenant = await this.tableStorageClient.RetrieveAsync<TenantModel>(
                                "tenant",
                                item.TenantId.Substring(0, 1),
                                item.TenantId);

                            Assembly assembly = Assembly.GetExecutingAssembly();
                            StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("grafana-main-dashboard.json"));
                            string template = await reader.ReadToEndAsync();
                            template = string.Format(
                                template,
                                this.config.ExternalDependencies.GrafanaUrl,
                                $"{adminDashboardUid}/{adminDashboardSubUrl}",
                                this.config.Global.SubscriptionId,
                                this.config.Global.ResourceGroup,
                                this.config.Global.LogAnalytics.Name,
                                $"IoT-{item.TenantId}",
                                mainDashboardUid,
                                mainDashboardName);
                            await this.grafanaClient.CreateAndUpdateDashboard(template);

                            reader = new StreamReader(assembly.GetManifestResourceStream("grafana-admin-dashboard.json"));
                            template = await reader.ReadToEndAsync();
                            template = string.Format(
                                template,
                                this.config.ExternalDependencies.GrafanaUrl,
                                $"{mainDashboardUid}/{mainDashboardSubUrl}",
                                this.config.Global.SubscriptionId,
                                this.config.Global.ResourceGroup,
                                this.config.Global.LogAnalytics.Name,
                                tenant.IotHubName,
                                this.config.Global.EventHub.Name,
                                this.config.Global.CosmosDb.AccountName,
                                adminDashboardUid,
                                adminDashboardName);

                            await this.grafanaClient.CreateAndUpdateDashboard(template);
                            await this.tableStorageClient.DeleteAsync(TableName, item);
                        }

                        if (item.Type == TenantOperation.GrafanaDashboardDeletion)
                        {
                            await this.grafanaClient.CreateAPIKeyIsNotFound();

                            string mainDashboardUid = item.TenantId.Substring(0, 8);

                            string adminDashboardUid = item.TenantId.Substring(item.TenantId.Length - 8, 8);

                            var tenant = await this.tableStorageClient.RetrieveAsync<TenantModel>(
                                "tenant",
                                item.TenantId.Substring(0, 1),
                                item.TenantId);

                            await this.grafanaClient.DeleteDashboardByUid(mainDashboardUid);

                            await this.grafanaClient.DeleteDashboardByUid(adminDashboardUid);
                            await this.tableStorageClient.DeleteAsync(TableName, item);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                await Task.Delay(15000, stoppingToken);
            }
        }

        private async Task<string> SetupEventHub(string tenantId)
        {
            string nameSpaceConnString = this.appConfigurationClient.GetValue($"tenant:{tenantId}:eventHubConn");
            string eventHubNameSpace = string.Format(EventHubNamespaceFormat, tenantId.Substring(0, 8));

            if (string.IsNullOrEmpty(nameSpaceConnString))
            {
                await this.azureManagementClient.EventHubsManagementClient.CreateNamespaceIfNotExist(eventHubNameSpace);
                nameSpaceConnString = await this.azureManagementClient.EventHubsManagementClient.GetPrimaryConnectionString(nameSpaceConnString);
                await this.appConfigurationClient.SetValueAsync($"tenant:{tenantId}:eventHubConn", nameSpaceConnString);
            }

            this.azureManagementClient.EventHubsManagementClient.CreateEventHub(eventHubNameSpace, $"{tenantId}-alerts");

            return eventHubNameSpace;
        }

        private async Task ADXAlertsSetup(string tenantId, string databaseName, string eventHubNameSpace)
        {
            Console.WriteLine($"Creating RawAlerts table and mapping in {tenantId} DB in Data Explorer");

            var tableName = "RawAlerts";
            var tableMappingName = $"RawAlertsMapping-{tenantId}";
            var tableSchema = new[]
            {
                  Tuple.Create("Data", "System.Object"),
            };
            var mappingSchema = new ColumnMapping[]
            {
                  new ColumnMapping() { ColumnName = "Data", ColumnType = "dynamic", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$" } } },
            };

            string dataConnectionName = $"AlertsDataConnect-{tenantId.Substring(0, 8)}";
            string eventHubName = $"{tenantId}-alerts";

            this.ADXTableSetup(tenantId, databaseName, tableName, tableSchema, tableMappingName, mappingSchema);

            this.kustoTableManagementClient.EnableStreamingIngestionPolicyToTable(tableName, databaseName);

            DataUpdatePolicy dataUpdatePolicy = new DataUpdatePolicy(
                                                isEnabled: false,
                                                source: null,
                                                query: null,
                                                isTransactional: true,
                                                propagateIngestionProperties: false);

            List<DataUpdatePolicy> policyList = new List<DataUpdatePolicy>();
            policyList.Add(dataUpdatePolicy);

            this.kustoTableManagementClient.AlterTablePolicy(tableName, databaseName, policyList);

            this.kustoTableManagementClient.AlterTableRetentionPolicy(tableName, databaseName, new TimeSpan(0, 0, 0, 0), DataRecoverability.Disabled);

            string consumerGroup = "$Default";

            await this.azureManagementClient.KustoClusterManagementClient.AddEventHubDataConnectionAsync(dataConnectionName, databaseName, tableName, tableMappingName, eventHubNameSpace, eventHubName, consumerGroup);

            var alertsTableName = "Alerts";
            var alertsTableMappingName = $"AlertsMapping-{tenantId}";
            var alertsTableSchema = new[]
            {
                  Tuple.Create("DeviceId", "System.String"),
                  Tuple.Create("MessageId", "System.String"),
                  Tuple.Create("Data", "System.Object"),
                  Tuple.Create("TimeStamp", "System.Datetime"),
            };
            var alertsMappingSchema = new ColumnMapping[]
            {
                  new ColumnMapping() { ColumnName = "DeviceId", ColumnType = "string", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.DeviceId" } } },
                  new ColumnMapping() { ColumnName = "MessageId", ColumnType = "string", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.MessageId" } } },
                  new ColumnMapping() { ColumnName = "Data", ColumnType = "dynamic", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.Data" } } },
                  new ColumnMapping() { ColumnName = "TimeStamp", ColumnType = "datetime", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.TimeStamp" } } },
            };

            this.ADXTableSetup(tenantId, databaseName, alertsTableName, alertsTableSchema, alertsTableMappingName, alertsMappingSchema);

            string functionName = "ProcessAlerts";
            string functionQuery = $@"{tableName} \n | project \n DeviceId = tostring(Data[""deviceId""]), MessageId = iif(isempty(tostring(Data[""MessageId""])), tostring(new_guid()), tostring(Data[""MessageId""])), Data = Data, TimeStamp = unixtime_milliseconds_todatetime(todouble(Data[""created""]))";

            this.kustoTableManagementClient.CreateOrAlterFunctionPolicy(functionName, null, functionQuery, databaseName);

            DataUpdatePolicy alertsDataUpdatePolicy = new DataUpdatePolicy(
                                                isEnabled: true,
                                                source: tableName,
                                                query: $"{functionName}()",
                                                isTransactional: true,
                                                propagateIngestionProperties: false);

            List<DataUpdatePolicy> alertsPolicyList = new List<DataUpdatePolicy>();
            policyList.Add(alertsDataUpdatePolicy);

            this.kustoTableManagementClient.AlterTablePolicy(alertsTableName, databaseName, alertsPolicyList);
        }

        private void ADXTableSetup(
            string tenantId,
            string databaseName,
            string tableName,
            Tuple<string, string>[] tableSchema,
            string tableMappingName,
            ColumnMapping[] mappingSchema)
        {
            this.kustoTableManagementClient.CreateTable(tableName, tableSchema, databaseName);

            this.kustoTableManagementClient.CreateTableMapping(tableMappingName, mappingSchema, tableName, databaseName);
        }
    }
}