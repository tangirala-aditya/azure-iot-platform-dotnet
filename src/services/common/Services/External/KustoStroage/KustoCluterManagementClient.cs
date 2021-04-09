// <copyright file="KustoCluterManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Kusto;
using Microsoft.Azure.Management.Kusto.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Common.Services.External.KustoStorage
{
    public class KustoCluterManagementClient : IKustoCluterManagementClient, IDisposable
    {
        private readonly ILogger logger;
        private AppConfig config;
        private IKustoManagementClient client;
        private bool disposedValue = false;

        public KustoCluterManagementClient(AppConfig config, ILogger<KustoCluterManagementClient> logger)
        {
            this.config = config;
            this.logger = logger;
            this.client = this.GetKustoClusterClient();
        }

        public async Task<StatusResultServiceModel> StatusAsync()
        {
            var result = new StatusResultServiceModel(false, "kusto check failed");

            try
            {
                Cluster response = null;
                if (this.client != null)
                {
                    // make generic call to see if kusto Manage client can be reached
                    response = await this.client.Clusters.GetAsync(
                        this.config.Global.ResourceGroup,
                        this.config.Global.DataExplorer.Name);
                }

                if (response != null)
                {
                    result.IsHealthy = true;
                    result.Message = "Alive and Well!";
                }
            }
            catch (Exception e)
            {
                this.logger.LogInformation(e, result.Message);
            }

            return result;
        }

        public async Task CreatedDBInCluterAsync(string databaseName, TimeSpan softDeletePeriod, TimeSpan? hotCachePeriod = null)
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
            catch (Exception e)
            {
                throw e;
            }
        }

        public IKustoManagementClient GetKustoClusterClient()
        {
            if (this.client == null)
            {
                try
                {
                    var authenticationContext = new AuthenticationContext($"https://login.windows.net/{this.config.Global.AzureActiveDirectory.TenantId}");

                    var credential = new ClientCredential(
                        this.config.Global.AzureActiveDirectory.AppId,
                        this.config.Global.AzureActiveDirectory.AppSecret);

                    var result = authenticationContext.AcquireTokenAsync(resource: "https://management.core.windows.net/", clientCredential: credential).Result;

                    var credentials = new TokenCredentials(result.AccessToken, result.AccessTokenType);

                    this.client = new KustoManagementClient(credentials)
                    {
                        SubscriptionId = this.config.Global.SubscriptionId,
                    };
                }
                catch (Exception e)
                {
                    var msg = "Unable to retrieve kusto with Active Directory properties" +
                          $"'{this.config.Global.AzureActiveDirectory.AppId}', '{this.config.Global.AzureActiveDirectory.AppSecret}' and '{this.config.Global.AzureActiveDirectory.TenantId}'.";
                    throw new InvalidConfigurationException(msg, e);
                }

                if (this.client == null)
                {
                    // this.logger.LogError(new Exception(errorMessage), errorMessage);
                    throw new InvalidConfigurationException("Could not connect to kusto client");
                }
            }

            return this.client;
        }

        public async Task AddEventHubDataConnectionAsync(string dataConnectName, string databaseName, string tableName, string tableMappingName, string eventHubName, string eventHubConsumerGroup)
        {
            try
            {
                await this.client.DataConnections.CreateOrUpdateAsync(
                        this.config.Global.ResourceGroup,
                        this.config.Global.DataExplorer.Name,
                        databaseName,
                        dataConnectName,
                        new EventHubDataConnection(
                            $"/subscriptions/{this.config.Global.SubscriptionId}/resourceGroups/{this.config.Global.ResourceGroup}/providers/Microsoft.EventHub/namespaces/{this.config.Global.EventHub.Name}/eventhubs/{eventHubName}",
                            eventHubConsumerGroup,
                            location: this.config.Global.Location,
                            tableName: tableName,
                            mappingRuleName: tableMappingName,
                            dataFormat: "JSON",
                            compression: "None"));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.client.Dispose();
                }

                this.disposedValue = true;
            }
        }
    }
}