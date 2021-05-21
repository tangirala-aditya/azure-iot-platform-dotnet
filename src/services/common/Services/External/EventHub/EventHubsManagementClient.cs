// <copyright file="EventHubsManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
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
    }
}