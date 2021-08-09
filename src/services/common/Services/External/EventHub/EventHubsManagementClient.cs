// <copyright file="EventHubsManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.EventHub;
using Mmm.Iot.Common.Services.Config;

namespace Mmm.Iot.Common.Services.External.EventHub
{
    public class EventHubsManagementClient
    {
        private readonly EventHubManagementClient eventHubManagementClient;
        private readonly AppConfig appConfig;

        public EventHubsManagementClient(EventHubManagementClient eventHubManagementClient, AppConfig appConfig)
        {
            this.eventHubManagementClient = eventHubManagementClient;
            this.appConfig = appConfig;
        }

        public void CreateEventHub(string namespaceName, string eventHubName, string resourceGroupName = null)
        {
            if (resourceGroupName == null)
            {
                resourceGroupName = this.appConfig.Global.ResourceGroup;
            }

            this.eventHubManagementClient.EventHubs.CreateOrUpdate(resourceGroupName, namespaceName, eventHubName, new Microsoft.Azure.Management.EventHub.Models.Eventhub() { MessageRetentionInDays = 1, PartitionCount = 1 });
        }

        public async Task<bool> CreateNamespace(string namespaceName, string resourceGroupName = null, string location = null)
        {
            bool result = false;
            if (string.IsNullOrWhiteSpace(resourceGroupName))
            {
                resourceGroupName = this.appConfig.Global.ResourceGroup;
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                location = this.appConfig.Global.Location;
            }

            try
            {
                await this.eventHubManagementClient.Namespaces.CreateOrUpdateAsync(resourceGroupName, namespaceName, new Microsoft.Azure.Management.EventHub.Models.EHNamespace()
                {
                    Location = location,
                    Sku = new Microsoft.Azure.Management.EventHub.Models.Sku("Standard", "Standard", 1),
                    IsAutoInflateEnabled = true,
                    MaximumThroughputUnits = 3,
                    KafkaEnabled = true,
                });
                result = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public async Task<string> GetPrimaryConnectionString(string namespaceName, string resourceGroupName = null)
        {
            if (resourceGroupName == null)
            {
                resourceGroupName = this.appConfig.Global.ResourceGroup;
            }

            var result = await this.eventHubManagementClient.Namespaces.ListKeysAsync(resourceGroupName, namespaceName, "RootManageSharedAccessKey");

            return result.PrimaryConnectionString;
        }

        public async Task DeleteEventHubNameSpace(string namespaceName, string resourceGroupName = null)
        {
            if (resourceGroupName == null)
            {
                resourceGroupName = this.appConfig.Global.ResourceGroup;
            }

            await this.eventHubManagementClient.Namespaces.DeleteAsync(resourceGroupName, namespaceName);
        }
    }
}