// <copyright file="KustoOperations.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Mmm.Iot.Functions.Messaging.Shared.Exceptions;
using Mmm.Iot.Functions.Messaging.Shared.Helpers;

namespace Mmm.Iot.Functions.Messaging.Shared
{
    public class KustoOperations
    {
        private const string ADXClusterNameKey = "Global:DataExplorer:Name";
        private const string LocationKey = "Global:Location";
        private const string ApplicationIdKey = "Global:AzureActiveDirectory:AppId";
        private const string ApplicationSecretKey = "Global:AzureActiveDirectory:AppSecret";
        private const string AppTenantKey = "Global:AzureActiveDirectory:TenantId";
        private static KustoOperations instance = null;
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ICslQueryProvider client = null;

        private KustoOperations(ICslQueryProvider client)
        {
            this.client = client;
        }

        public static async Task<KustoOperations> GetClientAsync()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return instance ?? (instance = CreateInstance());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<List<TDestination>> QueryAsync<TDestination>(
            string databaseName,
            string query,
            Dictionary<string, string> queryParameter)
        {
            try
            {
                var clientRequestProperties = new ClientRequestProperties();

                if (queryParameter != null)
                {
                    clientRequestProperties = new ClientRequestProperties(
                                    options: null,
                                    parameters: queryParameter);
                }

                clientRequestProperties.ClientRequestId = Guid.NewGuid().ToString();

                var result = await this.client.ExecuteQueryAsync(databaseName, query, clientRequestProperties);

                var resultList = result.ToJObjects().ToList();
                var queryResults = new List<TDestination>();
                foreach (var data in resultList)
                {
                    queryResults.Add(data.ToObject<TDestination>());
                }

                return queryResults;
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Error reading from kusto database {databaseName}", e);
            }
        }

        private static KustoOperations CreateInstance()
        {
            string connectionString = Environment.GetEnvironmentVariable("AppConfigurationConnectionString", EnvironmentVariableTarget.Process);

            AppConfigHelper appConfigHelper = new AppConfigHelper(connectionString);

            ICslQueryProvider docClient = CreateClient(appConfigHelper);
            return new KustoOperations(docClient);
        }

        private static ICslQueryProvider CreateClient(AppConfigHelper appConfigHelper)
        {
            string adxClustorName = appConfigHelper.GetValue(ADXClusterNameKey);
            string location = appConfigHelper.GetValue(LocationKey);
            string applicationId = appConfigHelper.GetValue(ApplicationIdKey);
            string applicationSecret = appConfigHelper.GetValue(ApplicationSecretKey);
            string appTenant = appConfigHelper.GetValue(AppTenantKey);

            try
            {
                var kustoUri = $"https://{adxClustorName}.{location}.kusto.windows.net/";

                var kustoConnectionStringBuilder = new KustoConnectionStringBuilder(kustoUri)
                    .WithAadApplicationKeyAuthentication(
                    applicationClientId: applicationId,
                    applicationKey: applicationSecret,
                    authority: appTenant);

                return KustoClientFactory.CreateCslQueryProvider(kustoConnectionStringBuilder);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("DocumentClient creation failed in the helper class", ex);
            }
        }
    }
}