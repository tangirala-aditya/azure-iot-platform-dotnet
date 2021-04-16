// <copyright file="KustoQueryClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Management.Kusto;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.KustoStroage;
using Mmm.Iot.Common.Services.External.TimeSeries;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Http;
using Mmm.Iot.Common.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Common.Services.External.KustoStorage
{
    public class KustoQueryClient : IKustoQueryClient
    {
        private const string ADEStorageTypeKey = "ade";
        private readonly string applicationId;
        private readonly string applicationSecret;
        private readonly string tenant;
        private readonly bool? kustoEnabled = null;
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
            if (!string.IsNullOrEmpty(config.DeviceTelemetryService.Messages.TelemetryStorageType))
            {
                this.kustoEnabled = config.DeviceTelemetryService.Messages.TelemetryStorageType.Equals(ADEStorageTypeKey, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                this.kustoEnabled = null;
            }

            this.client = this.GetKustoQueryClient();
        }

        public Task<StatusResultServiceModel> StatusAsync()
        {
            return null;
        }

        public async Task<List<TDestination>> QueryAsync<TDestination>(
            string dataBaseName,
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

                var result = await Task.FromResult(this.client.ExecuteQuery(dataBaseName, query, clientRequestProperties));

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
                throw e;
            }
        }

        public ICslQueryProvider GetKustoQueryClient()
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