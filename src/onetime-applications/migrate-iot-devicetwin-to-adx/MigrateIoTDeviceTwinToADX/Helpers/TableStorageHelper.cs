// <copyright file="TableStorageHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Mmm.Iot.MigrateIoTDeviceTwinToADX.Config;

namespace Mmm.Iot.MigrateIoTDeviceTwinToADX.Helpers
{
    public class TableStorageHelper
    {
        private readonly AppConfig config;
        private readonly CloudTableClient client;

        public TableStorageHelper(AppConfig config)
        {
            this.config = config;
            this.client = this.Create();
        }

        public CloudTableClient Create()
        {
            var storageAccount = CloudStorageAccount.Parse(this.config.Global.StorageAccountConnectionString);
            return storageAccount.CreateCloudTableClient();
        }

        public async Task<List<T>> QueryAsync<T>(
            string tableName,
            TableQuery<T> query,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : ITableEntity, new()
        {
            CloudTable table = await this.GetTableAsync(tableName);
            var items = new List<T>();
            TableContinuationToken token = null;
            do
            {
                // combine query segments until the full query has been executed or cancelled
                TableQuerySegment<T> seg = await table.ExecuteQuerySegmentedAsync<T>(query, token);
                token = seg.ContinuationToken;
                items.AddRange(seg);
            }
            while (token != null && !cancellationToken.IsCancellationRequested);
            return items;
        }

        private async Task<CloudTable> GetTableAsync(string tableName)
        {
            try
            {
                CloudTable table = this.client.GetTableReference(tableName);
                try
                {
                    await table.CreateIfNotExistsAsync();
                }
                catch (StorageException e)
                {
                    throw new Exception($"An error occurred during table.CreateIfNotExistsAsync for the {tableName} table.", e);
                }

                return table;
            }
            catch (StorageException se)
            {
                throw new Exception($"An error occurred while attempting to get the {tableName} table and checking if it needed to be created.", se);
            }
        }
    }
}