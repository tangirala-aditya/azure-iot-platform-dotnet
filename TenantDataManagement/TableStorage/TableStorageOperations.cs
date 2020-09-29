// <copyright file="TableStorageOperations.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace TenantDataManagement
{
    public class TableStorageOperations
    {
        private static TableStorageOperations instance = null;
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly CloudTableClient client = null;

        private TableStorageOperations(CloudTableClient cloudTableClient)
        {
            this.client = cloudTableClient;
        }

        public static async Task<TableStorageOperations> GetClientAsync(string connectionString)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return instance ?? (instance = CreateInstance(connectionString));
            }
            finally
            {
                semaphoreSlim.Release();
            }
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

        private static TableStorageOperations CreateInstance(string connectionString)
        {
            CloudTableClient tableClient = CreateClient(connectionString);
            return new TableStorageOperations(tableClient);
        }

        private static CloudTableClient CreateClient(string tableStorageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(tableStorageConnectionString);
            return storageAccount.CreateCloudTableClient();
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