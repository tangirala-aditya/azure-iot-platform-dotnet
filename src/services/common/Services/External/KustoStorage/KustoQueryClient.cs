// <copyright file="KustoQueryClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;

namespace Mmm.Iot.Common.Services.External.KustoStorage
{
    public class KustoQueryClient : IKustoQueryClient
    {
        private const string ADEStorageTypeKey = "ade";
        private readonly string applicationId;
        private readonly string applicationSecret;
        private readonly string tenant;
        private readonly ILogger logger;
        private ICslQueryProvider client;
        private AppConfig config;

        public KustoQueryClient(
            AppConfig config,
            ILogger<KustoQueryClient> logger)
        {
            this.logger = logger;
            this.config = config;
            this.applicationId = config.Global.AzureActiveDirectory.AppId;
            this.applicationSecret = config.Global.AzureActiveDirectory.AppSecret;
            this.tenant = config.Global.AzureActiveDirectory.TenantId;
            this.client = this.GetKustoQueryClient();
        }

        public KustoQueryClient(
            AppConfig config,
            ILogger<KustoQueryClient> logger,
            ICslQueryProvider kustQueryClient)
        {
            this.logger = logger;
            this.config = config;
            this.applicationId = config.Global.AzureActiveDirectory.AppId;
            this.applicationSecret = config.Global.AzureActiveDirectory.AppSecret;
            this.tenant = config.Global.AzureActiveDirectory.TenantId;
            this.client = kustQueryClient;
        }

        public async Task<List<TDestination>> ExecuteQueryAsync<TDestination>(
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

                result.Close();

                return queryResults;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error reading from kusto database {database}", databaseName);
                throw;
            }
        }

        public async Task<int> ExecuteCountQueryAsync(
            string databaseName,
            string query,
            Dictionary<string, string> queryParameter)
        {
            int count = 0;
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

                using var reader = await this.client.ExecuteQueryAsync(databaseName, query, clientRequestProperties);

                while (reader.Read())
                {
                    count = (int)reader.GetInt64(0);
                }

                return count;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error reading from kusto database {database}", databaseName);
                throw;
            }
        }

        private ICslQueryProvider GetKustoQueryClient()
        {
            if (this.client == null)
            {
                try
                {
                    var kustoUri = $"https://{this.config.Global.DataExplorer.Name}.{this.config.Global.Location}.kusto.windows.net/";

                    var kustoConnectionStringBuilder = new KustoConnectionStringBuilder(kustoUri)
                        .WithAadApplicationKeyAuthentication(
                        applicationClientId: this.applicationId,
                        applicationKey: this.applicationSecret,
                        authority: this.tenant);

                    this.client = KustoClientFactory.CreateCslQueryProvider(kustoConnectionStringBuilder);
                }
                catch (Exception e)
                {
                    var msg = "Unable to retrieve kusto with Active Directory properties" +
                          $"'{this.applicationId}'," +
                          $"'{this.applicationSecret}' and" +
                          $"'{this.tenant}'.";
                    throw new InvalidConfigurationException(msg, e);
                }

                if (this.client == null)
                {
                    // this.logger.LogError(new Exception(errorMessage), errorMessage);
                    throw new InvalidConfigurationException("Could not connect to kusto client");
                }
            }

            return this.client;
        }
    }
}