// <copyright file="Messages.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.External.KustoStorage;
using Mmm.Iot.Common.Services.External.KustoStroage;
using Mmm.Iot.Common.Services.External.TimeSeries;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Models;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.DeviceTelemetry.Services
{
    public class Messages : IMessages
    {
        private const string DataPropertyName = "data";
        private const string DataPrefix = DataPropertyName + ".";
        private const string SystemPrefix = "_";
        private const string DataSchemaType = DataPrefix + "schema";
        private const string DataPartitionId = "PartitionId";
        private const string TsiStorageTypeKey = "tsi";
        private const string TenantInfoKey = "tenant";
        private const string TelemetryCollectionKey = "telemetry-collection";
        private const string IoTDatabaseNameFormat = "IoT-{0}";
        private readonly ILogger logger;
        private readonly IStorageClient storageClient;
        private readonly ITimeSeriesClient timeSeriesClient;
        private readonly IKustoQueryClient kustoQueryClient;
        private readonly AppConfig config;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAppConfigurationClient appConfigurationClient;

        private readonly bool timeSeriesEnabled;
        private readonly IDocumentClient documentClient;
        private readonly string databaseName;

        public Messages(
            AppConfig config,
            IStorageClient storageClient,
            ITimeSeriesClient timeSeriesClient,
            IKustoQueryClient kustoQueryClient,
            ILogger<Messages> logger,
            IHttpContextAccessor contextAccessor,
            IAppConfigurationClient appConfigurationClient)
        {
            this.storageClient = storageClient;
            this.timeSeriesClient = timeSeriesClient;
            this.kustoQueryClient = kustoQueryClient;
            this.timeSeriesEnabled = config.DeviceTelemetryService.Messages.TelemetryStorageType.Equals(
                TsiStorageTypeKey, StringComparison.OrdinalIgnoreCase);
            this.documentClient = storageClient.GetDocumentClient();
            this.databaseName = config.DeviceTelemetryService.Messages.Database;
            this.logger = logger;
            this.config = config;
            this.httpContextAccessor = contextAccessor;
            this.appConfigurationClient = appConfigurationClient;
        }

        private string CollectionId
        {
            get
            {
                return this.appConfigurationClient.GetValue(
                    $"{TenantInfoKey}:{this.httpContextAccessor.HttpContext.Request.GetTenant()}:{TelemetryCollectionKey}");
            }
        }

        public async Task<MessageList> ListAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            InputValidator.Validate(order);
            foreach (var device in devices)
            {
                InputValidator.Validate(device);
            }

            switch (this.config.DeviceTelemetryService.Messages.TelemetryStorageType)
            {
                case TelemetryStorageTypeConstants.Tsi:
                    return await this.GetListFromTimeSeriesAsync(from, to, order, skip, limit, devices);
                case TelemetryStorageTypeConstants.Ade:
                    return await this.GetListFromKustoAsync(from, to, order, skip, limit, devices);
                default:
                    return await this.GetListFromCosmosDbAsync(from, to, order, skip, limit, devices);
            }
        }

        public async Task<MessageList> ListTopDeviceMessagesAsync(
            int limit,
            string deviceId)
        {
            InputValidator.Validate(deviceId);

            switch (this.config.DeviceTelemetryService.Messages.TelemetryStorageType)
            {
                case TelemetryStorageTypeConstants.Tsi:
                    return await this.GetListFromTimeSeriesAsync(limit, deviceId);
                case TelemetryStorageTypeConstants.Ade:
                    return await this.GetListFromKustoAsync(limit, deviceId);
                default:
                    return await this.GetListFromCosmosDbAsync(limit, deviceId);
            }
        }

        private async Task<MessageList> GetListFromCosmosDbAsync(
            int limit,
            string deviceId)
        {
            var sql = QueryBuilder.GetTopDeviceDocumentsSql("message", limit, deviceId, "deviceId");
            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = false,
                EnableScanInQuery = true,
            };

            List<Document> docs = new List<Document>();
            try
            {
                docs = await this.storageClient.QueryDocumentsAsync(
                      this.databaseName,
                      this.CollectionId,
                      queryOptions,
                      sql,
                      0,
                      limit);
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No telemetry messages exist in CosmosDb. The telemetry collection {this.CollectionId} does not exist.", e);
            }

            // Messages to return
            List<Message> messages = new List<Message>();

            // Auto discovered telemetry types
            HashSet<string> properties = new HashSet<string>();

            foreach (Document doc in docs)
            {
                // Document fields to expose
                JObject data = new JObject();

                // Extract all the telemetry data and types
                var jsonDoc = JObject.Parse(doc.ToString());

                foreach (var item in jsonDoc)
                {
                    // Ignore fields that werent sent by device (system fields)"
                    if (!item.Key.StartsWith(SystemPrefix) && item.Key != "id" && item.Key != "deviceId")
                    {
                        string key = item.Key.ToString();
                        data.Add(key, item.Value);

                        // Telemetry types auto-discovery magic through union of all keys
                        properties.Add(key);
                    }
                }

                messages.Add(new Message(
                    doc.GetPropertyValue<string>("deviceId"),
                    doc.GetPropertyValue<long>("_timeReceived"),
                    data));
            }

            return new MessageList(messages, new List<string>(properties));
        }

        private async Task<MessageList> GetListFromTimeSeriesAsync(
            int limit,
            string deviceId)
        {
            return await this.timeSeriesClient.QueryEventsAsync(limit, deviceId);
        }

        private async Task<MessageList> GetListFromTimeSeriesAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            return await this.timeSeriesClient.QueryEventsAsync(from, to, order, skip, limit, devices);
        }

        private async Task<MessageList> GetListFromKustoAsync(
            int limit,
            string deviceId)
        {
            // TODO
            string database = $"IoT-{this.httpContextAccessor.HttpContext.Request.GetTenant()}";

            (string query, Dictionary<string, string> queryParameter) = QueryBuilder.GetTopDeviceMessagesKustoQuery(
                "Telemetry",
                limit,
                "desc",
                "TimeStamp",
                deviceId,
                "DeviceId");

            var results = await this.kustoQueryClient.ExecuteQueryAsync<TelemetryModel>(database, query, queryParameter);

            HashSet<string> properties = new HashSet<string>();
            var messages = new List<Message>();

            foreach (var result in results)
            {
                JObject data = new JObject();

                // Extract all the telemetry data and types
                var jsonDoc = JObject.Parse(result.Data.ToString());

                foreach (var item in jsonDoc)
                {
                    // Ignore fields that werent sent by device (system fields)"
                    if (!item.Key.StartsWith("_") && !item.Key.StartsWith("iothub-") && item.Key != "id" && item.Key != "deviceId")
                    {
                        string key = item.Key.ToString();
                        data.Add(key, item.Value);

                        properties.Add(key);
                    }
                }

                messages.Add(new Message
                {
                    DeviceId = result.DeviceId,
                    Time = result.TimeStamp,
                    Data = data,
                });
            }

            return new MessageList(messages, new List<string>(properties));
        }

        private async Task<MessageList> GetListFromKustoAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            string database = string.Format(IoTDatabaseNameFormat, this.httpContextAccessor.HttpContext.Request.GetTenant());

            (string query, Dictionary<string, string> queryParameter) = QueryBuilder.GetKustoQuery(
                "Telemetry",
                null,
                null,
                from,
                "TimeStamp",
                to,
                "TimeStamp",
                order,
                "TimeStamp",
                skip,
                limit,
                devices,
                "DeviceId");

            var results = await this.kustoQueryClient.ExecuteQueryAsync<TelemetryModel>(database, query, queryParameter);

            HashSet<string> properties = new HashSet<string>();
            var messages = new List<Message>();

            foreach (var result in results)
            {
                JObject data = new JObject();

                // Extract all the telemetry data and types
                var jsonDoc = JObject.Parse(result.Data.ToString());

                foreach (var item in jsonDoc)
                {
                    // Ignore fields that werent sent by device (system fields)"
                    if (!item.Key.StartsWith("_") && !item.Key.StartsWith("iothub-") && item.Key != "id" && item.Key != "deviceId")
                    {
                        string key = item.Key.ToString();
                        data.Add(key, item.Value);

                        properties.Add(key);
                    }
                }

                messages.Add(new Message
                {
                    DeviceId = result.DeviceId,
                    Time = result.TimeStamp,
                    Data = data,
                });
            }

            return new MessageList(messages, new List<string>(properties));
        }

        private async Task<MessageList> GetListFromCosmosDbAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            int dataPrefixLen = DataPrefix.Length;

            var sql = QueryBuilder.GetDocumentsSql(
                "message",
                null,
                null,
                from,
                "_timeReceived",
                to,
                "_timeReceived",
                order,
                "_timeReceived",
                skip,
                limit,
                devices,
                "deviceId");

            this.logger.LogDebug("Created message query {sql}", sql);

            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                EnableScanInQuery = true,
            };

            List<Document> docs = new List<Document>();
            try
            {
                docs = await this.storageClient.QueryDocumentsAsync(
                    this.databaseName,
                    this.CollectionId,
                    queryOptions,
                    sql,
                    skip,
                    limit);
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No telemetry messages exist in CosmosDb. The telemetry collection {this.CollectionId} does not exist.", e);
            }

            // Messages to return
            List<Message> messages = new List<Message>();

            // Auto discovered telemetry types
            HashSet<string> properties = new HashSet<string>();

            foreach (Document doc in docs)
            {
                // Document fields to expose
                JObject data = new JObject();

                // Extract all the telemetry data and types
                var jsonDoc = JObject.Parse(doc.ToString());

                foreach (var item in jsonDoc)
                {
                    // Ignore fields that werent sent by device (system fields)"
                    if (!item.Key.StartsWith(SystemPrefix) && item.Key != "id" && item.Key != "deviceId")
                    {
                        string key = item.Key.ToString();
                        data.Add(key, item.Value);

                        // Telemetry types auto-discovery magic through union of all keys
                        properties.Add(key);
                    }
                }

                messages.Add(new Message(
                    doc.GetPropertyValue<string>("deviceId"),
                    doc.GetPropertyValue<long>("_timeReceived"),
                    data));
            }

            return new MessageList(messages, new List<string>(properties));
        }
    }
}