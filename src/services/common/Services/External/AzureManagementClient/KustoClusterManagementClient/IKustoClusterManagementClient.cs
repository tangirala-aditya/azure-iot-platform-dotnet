// <copyright file="IKustoClusterManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public interface IKustoClusterManagementClient : IStatusOperation
    {
        Task CreateDBInClusterAsync(string databaseName, TimeSpan? softDeletePeriod, TimeSpan? hotCachePeriod = null);

        Task AddEventHubDataConnectionAsync(string dataConnectName, string databaseName, string tableName, string tableMappingName, string eventHubNamespace, string eventHubName, string eventHubConsumerGroup);

        Task AddIoTHubDataConnectionAsync(string dataConnectName, string databaseName, string tableName, string tableMappingName, string iotHubName, string iotHubConsumerGroup);

        Task DeleteDatabaseAsync(string databaseName);
    }
}