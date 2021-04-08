// <copyright file="ADEClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Azure.Management.Kusto;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.TimeSeries;
using Mmm.Iot.Common.Services.Http;
using Mmm.Iot.Common.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Common.Services.External.ADE
{
    public class ADEClient : IADEClient
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
        private AuthenticationResult token;

        public ADEClient(
            IHttpClient httpClient,
            AppConfig config,
            ILogger<ADEClient> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
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
        }

        public Task<StatusResultServiceModel> StatusAsync()
        {
            return null;
        }

        public async Task<MessageList> QueryADEDb(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] deviceIds)
        {
            try
            {
                const string Cluster = "https://sahithicluster.eastus.kusto.windows.net";
                const string Database = "testDB";
                int number = 0;
                string newList = string.Empty;

                // Recommended syntax - AAD User token
                string accessToken = await this.AcquireAccessTokenAsync();
                var kcsb = new KustoConnectionStringBuilder(Cluster, Database)
                   .WithAadUserTokenAuthentication(accessToken);

                var kcsb1 = new KustoConnectionStringBuilder(Cluster, Database)
                    .WithAadUserPromptAuthentication();
                /*
                                var tenantId = "7896c5aa-71b4-47c3-98d4-e707bdd7462f"; // Directory (tenant) ID
                                var clientId = "b34faa19-7f22-4feb-8e66-8cc124671b39"; // Application ID
                                var clientSecret = "xY.uMBH~E~1CccK~vdjtq.Q2tA8ne35.hY"; // Client Secret
                                var subscriptionId = "f24c7600-f18b-4d5e-a974-0f6af4f7ce31";
                                var authenticationContext = new AuthenticationContext($"https://login.windows.net/{tenantId}");
                                var credential = new ClientCredential(clientId, clientSecret);
                                var result = authenticationContext.AcquireTokenAsync(resource: "https://management.core.windows.net/", clientCredential: credential).Result;

                                var credentials = new TokenCredentials(result.AccessToken, result.AccessTokenType);

                                var kustoManagementClient = new KustoManagementClient(credentials)
                                {
                                    SubscriptionId = subscriptionId,
                                };

                                // var serviceUri = "Service URI, typically of the form https://cluster.region.kusto.windows.net";
                                var access_token = result.AccessToken;
                                */

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

                using (var queryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb1))
                {
                    MessageList listModel = new MessageList();

                    // Auto discovered telemetry types
                    HashSet<string> properties = new HashSet<string>();
                    var query = string.Format("testTable | where deviceId in ({0})", newList);
                    var clientRequestProperties = new ClientRequestProperties() { ClientRequestId = Guid.NewGuid().ToString() };
                    using (var reader = queryProvider.ExecuteQuery(query, clientRequestProperties))
                    {
                        while (reader.Read())
                        {
                            var ty = new Message();
                            ty.Data = (JObject)reader[1];
                            ty.Time = DateTimeOffset.UtcNow;
                            ty.DeviceId = Convert.ToString(reader[0]);

                            listModel.Messages.Add(ty);

                            // listModel.Messages.Add(new Message
                            // {
                            //    DeviceId = Convert.ToString(reader[0]),
                            //    Time = DateTimeOffset.UtcNow,
                            //    Data = (JObject)reader[1],
                            // );
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

        private async Task<string> AcquireAccessTokenAsync()
        {
            // Return existing token unless it is near expiry or null
            if (this.token != null)
            {
                // Add buffer time to renew token, built in buffer for AAD is 5 mins
                if (DateTimeOffset.UtcNow.AddSeconds(ClockCalibrationInSeconds) < this.token.ExpiresOn)
                {
                    return this.token.AccessToken;
                }
            }

            if (string.IsNullOrEmpty(this.applicationId) ||
                string.IsNullOrEmpty(this.applicationSecret) ||
                string.IsNullOrEmpty(this.tenant))
            {
                throw new InvalidConfigurationException(
                    $"Active Directory properties '{AadClientIdKey}', '{AadClientSecretKey}' " +
                    $"and '{AadTenantKey}' are not set.");
            }

            var authenticationContext = new AuthenticationContext(
                this.authority + this.tenant,
                TokenCache.DefaultShared);

            try
            {
                AuthenticationResult tokenResponse = await authenticationContext.AcquireTokenAsync(
                    resource: this.host,
                    clientCredential: new ClientCredential(
                        clientId: this.applicationId,
                        clientSecret: this.applicationSecret));

                this.token = tokenResponse;

                return this.token.AccessToken;
            }
            catch (Exception e)
            {
                var msg = "Unable to retrieve token with Active Directory properties" +
                          $"'{AadClientIdKey}', '{AadClientSecretKey}' and '{AadTenantKey}'.";
                throw new InvalidConfigurationException(msg, e);
            }
        }
    }
}