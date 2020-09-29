// <copyright file="TenantDataCollectionService.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;

namespace TenantDataManagement
{
    public class TenantDataCollectionService
    {
        private readonly IAzure client;
        private DateTime cacheExpiration;
        private AzureCredentials azureCredentials;
        private ResourceManagementClient rmClient;
        private StorageManagementClient storageClient;

        public TenantDataCollectionService()
        {
            this.cacheExpiration = DateTime.UnixEpoch;
            this.client = this.Create();
            this.rmClient = (ResourceManagementClient)this.client.ManagementClients.FirstOrDefault(t =>
            t.GetType() == typeof(ResourceManagementClient));
            this.storageClient = (StorageManagementClient)this.client.ManagementClients.FirstOrDefault(t => t.GetType() == typeof(StorageManagementClient));
        }

        private AzureCredentials AzureCredentials
        {
            get
            {
                if (DateTime.Compare(this.cacheExpiration, DateTime.Now) < 0)
                {
                    this.cacheExpiration = DateTime.Now.AddMinutes(1);

                    var servicePrincipal = new ServicePrincipalLoginInformation();
                    servicePrincipal.ClientId = Environment.GetEnvironmentVariable("AppId", EnvironmentVariableTarget.Process);
                    servicePrincipal.ClientSecret = Environment.GetEnvironmentVariable("AppSecret", EnvironmentVariableTarget.Process);
                    this.azureCredentials = new AzureCredentials(servicePrincipal, string.Empty, AzureEnvironment.AzureGlobalCloud);
                }

                return this.azureCredentials;
            }

            set
            {
                this.azureCredentials = value;
            }
        }

        public Microsoft.Azure.Management.Fluent.IAzure Create()
        {
            string subscriptionId = Environment.GetEnvironmentVariable("SubscriptionId", EnvironmentVariableTarget.Process);

            var azure = Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .Authenticate(this.AzureCredentials)
                .WithSubscription(subscriptionId);
            return azure;
        }

        public async Task<object> GetResourceGroups()
        {
            var resourceGroups = (await this.rmClient.ResourceGroups.ListAsync()).ToList();

            foreach (var resourceGroup in resourceGroups)
            {
                await this.GetResourcesByResourceGroups(resourceGroup.Name);
            }

            return resourceGroups;
        }

        public async Task<object> GetResourcesByResourceGroups(string resourceGroupName)
        {
            var x = (await this.rmClient.Resources.ListByResourceGroupAsync(resourceGroupName)).ToList();
            string storageAccountName = x.Where(y => y.Kind == "Storage").ToList()[0].Name;
            string storageAccConnectionString = this.GetStorageAccountConnectionString(resourceGroupName, storageAccountName);
            return x;
        }

        public string GetStorageAccountConnectionString(string resourceGroupName, string storageAccountName)
        {
            IList<StorageAccountKey> acctKeys = this.storageClient.StorageAccounts.ListKeysAsync(resourceGroupName, storageAccountName).Result.Keys;
            return $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={acctKeys.FirstOrDefault()?.Value};EndpointSuffix=core.windows.net";
        }

        public async Task<object> GetAzureReourcesData()
        {
            return await this.GetResourcesByResourceGroups("rg-iot-ggk-dev");
        }
    }
}