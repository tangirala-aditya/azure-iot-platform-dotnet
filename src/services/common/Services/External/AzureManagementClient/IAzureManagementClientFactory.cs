// <copyright file="IAzureManagementClientFactory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Management.IotHub;
using Mmm.Iot.Common.Services.External.EventHub;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public interface IAzureManagementClientFactory
    {
        Microsoft.Azure.Management.Fluent.IAzure Create();

        IoTHubManagementClient CreateIoTHubManagementClient();

        AsaManagementClient CreateAsaManagementClient();

        KustoClusterManagementClient CreateKustoClusterManagementClient();

        EventHubsManagementClient CreateEventHubsManagementClient();
    }
}