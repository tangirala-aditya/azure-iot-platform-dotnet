// <copyright file="KustoQueryClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private const string TsiDateFormat = "yyyy-MM-ddTHH:mm:ssZ";
        private const string TimeSeriesApiVersionPrefix = "api-version";
        private const string TimeSeriesTimeoutPrefix = "timeout";
        private const string EventsKey = "events";
        private const string AvailabilityKey = "availability";
        private const string SearchSpanKey = "searchSpan";
        private const string PredicateKey = "predicate";
        private const string PredicateStringKey = "predicateString";
        private const string TopKey = "top";
        private const string SortKey = "sort";
        private const string SortInputKey = "input";
        private const string BuiltInPropKey = "builtInProperty";
        private const string BuiltInPropValue = "$ts";
        private const string SortOrderKey = "order";
        private const string CountKey = "count";
        private const string FromKey = "from";
        private const string ToKey = "to";
        private const int ClockCalibrationInSeconds = 5;
        private const string DeviceIdKey = "iothub-connection-device-id";
        private const string AadClientIdKey = "ApplicationClientId";
        private const string AadClientSecretKey = "ApplicationClientSecret";
        private const string AadTenantKey = "Tenant";
        private const string TsiStorageTypeKey = "tsi";
        private readonly string authority;
        private readonly string applicationId;
        private readonly string applicationSecret;
        private readonly string tenant;
        private readonly string fqdn;
        private readonly string host;
        private readonly string apiVersion;
        private readonly bool? timeSeriesEnabled = null;
        private readonly string timeout;
        private readonly IHttpClient httpClient;
        private readonly ILogger logger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private ICslQueryProvider client;
        private AppConfig config;

        public KustoQueryClient(
            IHttpClient httpClient,
            AppConfig config,
            ILogger<KustoQueryClient> logger,
            IHttpContextAccessor contextAccessor)
        {
            this.httpClient = httpClient;
            this.logger = logger;
            this.config = config;
            this.authority = config.DeviceTelemetryService.TimeSeries.Authority;
            this.applicationId = config.Global.AzureActiveDirectory.AppId;
            this.applicationSecret = config.Global.AzureActiveDirectory.AppSecret;
            this.tenant = config.Global.AzureActiveDirectory.TenantId;
            this.fqdn = config.DeviceTelemetryService.TimeSeries.TsiDataAccessFqdn;
            this.host = config.DeviceTelemetryService.TimeSeries.Audience;
            this.apiVersion = config.DeviceTelemetryService.TimeSeries.ApiVersion;
            this.timeout = config.DeviceTelemetryService.TimeSeries.Timeout;
            if (!string.IsNullOrEmpty(config.DeviceTelemetryService.Messages.TelemetryStorageType))
            {
                this.timeSeriesEnabled = config.DeviceTelemetryService.Messages.TelemetryStorageType.Equals(TsiStorageTypeKey, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                this.timeSeriesEnabled = null;
            }

            this.httpContextAccessor = contextAccessor;
            this.client = this.GetKustoQueryClient();
        }

        public Task<StatusResultServiceModel> StatusAsync()
        {
            return null;
        }

        public async Task<MessageList> QueryAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] deviceIds)
        {
            try
            {
                // TODO
                var sql = QueryBuilder.GetKustoQuery(
                "telemetry",
                from,
                "timeStamp",
                to,
                "timeStamp",
                order,
                "timeStamp",
                skip,
                limit,
                deviceIds,
                "deviceId");

                string database = this.httpContextAccessor.HttpContext.Request.GetTenant();
                int number = 0;
                string newList = string.Empty;

                // build deviceId string
                foreach (var item in deviceIds)
                {
                    if (number > 0)
                    {
                        newList = newList + "," + "'" + item.Trim() + "'";
                    }
                    else
                    {
                        newList = "'" + item + "'";
                    }

                    number++;
                }

                MessageList listModel = new MessageList();

                // Auto discovered telemetry types
                HashSet<string> properties = new HashSet<string>();
                var query = string.Format("telemetry | where deviceId in ({0})", newList);
                var clientRequestProperties = new ClientRequestProperties() { ClientRequestId = Guid.NewGuid().ToString() };

                using (var reader = await this.client.ExecuteQueryAsync(database, query, clientRequestProperties))
                {
                    while (reader.Read())
                    {
                        var data = new Message();
                        data.Data = (JObject)reader[1];
                        data.Time = DateTimeOffset.Parse(Convert.ToString(reader[2]));
                        data.DeviceId = Convert.ToString(reader[0]);

                        listModel.Messages.Add(data);

                        foreach (var item in JObject.Parse(reader[1].ToString()))
                        {
                            // Ignore fields that werent sent by device (system fields)"
                            if (!item.Key.StartsWith('_') && item.Key != "id" && item.Key != "deviceId")
                            {
                                string key = item.Key.ToString();
                                properties.Add(key);
                            }
                        }
                    }
                }

                foreach (string s in properties)
                {
                    listModel.Properties.Add(s);
                }

                return listModel;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public Task<MessageList> QueryEventsAsync(int limit, string deviceId)
        {
            throw new NotImplementedException();
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