// <copyright file="KustoTableManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Common.Services.External.KustoStorage
{
    public class KustoTableManagementClient : IKustoTableManagementClient
    {
        private readonly ILogger logger;
        private AppConfig config;
        private ICslAdminProvider client;
        private bool disposedValue = false;

        public KustoTableManagementClient(AppConfig config, ILogger<KustoTableManagementClient> logger)
        {
            this.config = config;
            this.logger = logger;
            this.client = this.GetKustoAdminClient();
        }

        public Task<StatusResultServiceModel> StatusAsync()
        {
            return null;
        }

        public ICslAdminProvider GetKustoAdminClient()
        {
            if (this.client == null)
            {
                try
                {
                    var kustoUri = $"https://{this.config.Global.DataExplorer.Name}.{this.config.Global.Location}.kusto.windows.net/";

                    var kustoConnectionStringBuilder = new KustoConnectionStringBuilder(kustoUri)
                        .WithAadApplicationKeyAuthentication(
                        applicationClientId: this.config.Global.AzureActiveDirectory.AppId,
                        applicationKey: this.config.Global.AzureActiveDirectory.AppSecret,
                        authority: this.config.Global.AzureActiveDirectory.TenantId);

                    this.client = KustoClientFactory.CreateCslAdminProvider(kustoConnectionStringBuilder);
                }
                catch (Exception e)
                {
                    var msg = "Unable to retrieve kusto with Active Directory properties" +
                          $"'{this.config.Global.AzureActiveDirectory.AppId}'," +
                          $"'{this.config.Global.AzureActiveDirectory.AppSecret}' and" +
                          $"'{this.config.Global.AzureActiveDirectory.TenantId}'.";
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

        public void CreateTable(string tableName, IEnumerable<Tuple<string, string>> rowFields, string databaseName)
        {
            try
            {
                var command = CslCommandGenerator.GenerateTableCreateCommand(
                                                tableName,
                                                rowFields);

                this.client.ExecuteControlCommand(databaseName, command);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void CreateTableMapping(string tableMappingName, IEnumerable<ColumnMapping> mapping, string tableName, string databaseName)
        {
            try
            {
                var command = CslCommandGenerator.GenerateTableMappingCreateCommand(
                           Kusto.Data.Ingestion.IngestionMappingKind.Json,
                           tableName,
                           tableMappingName,
                           mapping);

                this.client.ExecuteControlCommand(databaseName, command);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.client.Dispose();
                }

                this.disposedValue = true;
            }
        }
    }
}