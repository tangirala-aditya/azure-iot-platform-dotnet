// <copyright file="IKustoCluterManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mmm.Iot.Common.Services.External.KustoStorage
{
    public interface IKustoCluterManagementClient : IStatusOperation
    {
        Task CreatedDBInCluterAsync(string databaseName, TimeSpan softDeletePeriod, TimeSpan? hotCachePeriod = null);

        Task AddEventHubDataConnectionAsync(string dataConnectName, string databaseName, string tableName, string tableMappingName, string eventHubName, string eventHubConsumerGroup);
    }
}