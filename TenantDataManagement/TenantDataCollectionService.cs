// <copyright file="TenantDataCollectionService.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.IotHub;
using Microsoft.Azure.Management.IotHub.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Rest.Azure;

namespace TenantDataManagement
{
    public class TenantDataCollectionService : IDisposable
    {
        private const int MaxDeployments = 100;
        private readonly IAzure client;
        private DateTime cacheExpiration;
        private AzureCredentials azureCredentials;
        private ResourceManagementClient rmClient;
        private StorageManagementClient storageClient;
        private IotHubClient iotHubClient;

        public TenantDataCollectionService()
        {
            this.cacheExpiration = DateTime.UnixEpoch;
            this.client = this.Create();
            this.rmClient = (ResourceManagementClient)this.client.ManagementClients.FirstOrDefault(t =>
            t.GetType() == typeof(ResourceManagementClient));
            this.storageClient = (StorageManagementClient)this.client.ManagementClients.FirstOrDefault(t => t.GetType() == typeof(StorageManagementClient));
            this.iotHubClient = new IotHubClient(this.AzureCredentials);
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

        public async Task<object> GetAzureReourcesData()
        {
            return await this.GetResourcesByResourceGroups("rg-iot-ggk-dev");
        }

        public void Dispose()
        {
            if (this.iotHubClient != null)
            {
                this.iotHubClient.Dispose();
                this.iotHubClient = null;
            }

            if (this.storageClient != null)
            {
                this.storageClient.Dispose();
                this.storageClient = null;
            }

            if (this.rmClient != null)
            {
                this.rmClient.Dispose();
                this.rmClient = null;
            }
        }

        private async Task<object> GetResourcesByResourceGroups(string resourceGroupName)
        {
            var x = (await this.rmClient.Resources.ListByResourceGroupAsync(resourceGroupName)).ToList();
            string storageAccountName = x.Where(y => y.Kind == "Storage").ToList()[0].Name;
            string storageAccConnectionString = this.GetStorageAccountConnectionString(resourceGroupName, storageAccountName);
            this.FetchTenantDetailsFromTableStorage(storageAccConnectionString, resourceGroupName);
            return x;
        }

        private string GetStorageAccountConnectionString(string resourceGroupName, string storageAccountName)
        {
            IList<StorageAccountKey> acctKeys = this.storageClient.StorageAccounts.ListKeysAsync(resourceGroupName, storageAccountName).Result.Keys;
            return $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={acctKeys.FirstOrDefault()?.Value};EndpointSuffix=core.windows.net";
        }

        private async void FetchTenantDetailsFromTableStorage(string storageAccConnectionString, string resourceGroupName)
        {
            List<AzureTenantData> azureTenantDataList = new List<AzureTenantData>();
            TableStorageOperations cloudTableClient = await TableStorageOperations.GetClientAsync(storageAccConnectionString);
            TableQuery<TenantModel> query = new TableQuery<TenantModel>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.NotEqual, string.Empty));
            List<TenantModel> tenants = await cloudTableClient.QueryAsync<TenantModel>("tenant", query);

            foreach (TenantModel tenant in tenants)
            {
                AzureTenantData azureTenantData = new AzureTenantData();

                this.HydrateTenantMetaData(azureTenantData, tenant, resourceGroupName);

                await this.HydrateUserData(cloudTableClient, tenant, azureTenantData);

                await this.HydrateUserAccessData(cloudTableClient, tenant, azureTenantData);

                await this.HydrateLocationInformationFromIoTHub(azureTenantData);

                await this.HydrateContentInformationFromIoTHub(azureTenantData);

                azureTenantDataList.Add(azureTenantData);
            }
        }

        private async Task HydrateUserAccessData(TableStorageOperations cloudTableClient, TenantModel tenant, AzureTenantData azureTenantData)
        {
            TableQuery<UserSettingsModel> userSettingsQuery = new TableQuery<UserSettingsModel>().Where(
                TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "LastusedTenant"),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("Value", QueryComparisons.Equal, tenant.TenantId)));

            List<UserSettingsModel> userSettings = await cloudTableClient.QueryAsync<UserSettingsModel>("userSettings", userSettingsQuery);
            UserSettingsModel userSettingsModelForSelectedTenant = userSettings.OrderByDescending(z => z.Timestamp).FirstOrDefault();

            azureTenantData.LastAccessed = userSettingsModelForSelectedTenant?.Timestamp;
        }

        private async Task HydrateUserData(TableStorageOperations cloudTableClient, TenantModel tenant, AzureTenantData azureTenantData)
        {
            TableQuery<UserTenantModel> userQuery = new TableQuery<UserTenantModel>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, tenant.TenantId));
            List<UserTenantModel> users = await cloudTableClient.QueryAsync<UserTenantModel>("user", userQuery);
            azureTenantData.UserCount = users.Count;
        }

        private void HydrateTenantMetaData(AzureTenantData azureTenantData, TenantModel tenant, string resourceGroupName)
        {
            azureTenantData.ResourceGroup = resourceGroupName;
            azureTenantData.TenantName = tenant.TenantName;
            azureTenantData.TenantId = tenant.TenantId;
            azureTenantData.SAJob = tenant.SAJobName;
            azureTenantData.IoTHubIsDeployed = tenant.IsIotHubDeployed;
            azureTenantData.IoTHubName = tenant.IotHubName;
        }

        private async Task<IotHubDescription> RetrieveAsync(string resourceGroupName, string iotHubName)
        {
            return await this.iotHubClient.IotHubResource.GetAsync(resourceGroupName, iotHubName, CancellationToken.None);
        }

        private async Task HydrateLocationInformationFromIoTHub(AzureTenantData azureTenantData)
        {
            var iotHubInfo = await this.RetrieveAsync(azureTenantData.ResourceGroup, azureTenantData.IoTHubName);

            if (iotHubInfo != null)
            {
                azureTenantData.Region = iotHubInfo.Location;
            }
        }

        private async Task HydrateContentInformationFromIoTHub(AzureTenantData azureTenantData)
        {
            string connString = this.GetIoTHubConnectionString(azureTenantData.ResourceGroup, azureTenantData.IoTHubName);
            RegistryManager registry = RegistryManager.CreateFromConnectionString(connString);

            Microsoft.Azure.Devices.RegistryStatistics stats = await registry.GetRegistryStatisticsAsync();
            azureTenantData.DeviceCount = stats.TotalDeviceCount;

            var deployments = await registry.GetConfigurationsAsync(MaxDeployments);
            azureTenantData.DeploymentCount = deployments != null ? deployments.Count() : 0;
        }

        private IPage<SharedAccessSignatureAuthorizationRule> ListKeysAsync(string resourceGroupName, string iotHubName)
        {
            return this.iotHubClient.IotHubResource.ListKeys(resourceGroupName, iotHubName);
        }

        private string GetAccessKey(string resourceGroupName, string iotHubName)
        {
            var keys = this.ListKeysAsync(resourceGroupName, iotHubName);
            return keys.Where(t => t.KeyName == "iothubowner").FirstOrDefault().PrimaryKey;
        }

        private string GetIoTHubConnectionString(string resourceGroupName, string iotHubName)
        {
            return $"HostName={iotHubName}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey={this.GetAccessKey(resourceGroupName, iotHubName)}";
        }
    }
}