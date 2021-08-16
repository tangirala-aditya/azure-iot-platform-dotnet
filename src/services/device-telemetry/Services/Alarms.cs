// <copyright file="Alarms.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.DeviceTelemetry.Services.Helper;
using Mmm.Iot.DeviceTelemetry.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EventHubHelper = Mmm.Iot.DeviceTelemetry.Services.Helper.EventHubHelper;

namespace Mmm.Iot.DeviceTelemetry.Services
{
    public class Alarms : IAlarms
    {
        private const string MessageReceivedKey = "deviceMsgReceived";
        private const string RuleIdKey = "ruleId";
        private const string DeviceIdKey = "deviceId";
        private const string StatusKey = "status";
        private const string AlarmSchemaKey = "alarm";
        private const string AlarmStatusOpen = "open";
        private const string AlarmStatusAcknowledged = "acknowledged";
        private const string TenantInfoKey = "tenant";
        private const string AlarmsCollectionKey = "alarms-collection";
        private const string IoTDatabaseNameFormat = "IoT-{0}";
        private const string DateModifiedKustoKey = "DateModified";
        private const string AlertsKustoTableKey = "Alerts";
        private const string StatusKustoKey = "Status";
        private const string DeviceIdKustoKey = "DeviceId";
        private const string RuleIdKustoKey = "RuleId";
        private const int DocumentQueryLimit = 1000;
        private readonly string databaseName;
        private readonly int maxDeleteRetryCount;
        private readonly ILogger logger;
        private readonly IStorageClient storageClient;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAppConfigurationClient appConfigurationClient;
        private readonly IKustoQueryClient kustoQueryClient;
        private readonly bool kustoEnabled;

        public Alarms(
            AppConfig config,
            IStorageClient storageClient,
            ILogger<Alarms> logger,
            IHttpContextAccessor contextAccessor,
            IAppConfigurationClient appConfigurationClient,
            IKustoQueryClient kustoQueryClient)
        {
            this.storageClient = storageClient;
            this.databaseName = config.DeviceTelemetryService.Alarms.Database;
            this.logger = logger;
            this.maxDeleteRetryCount = config.DeviceTelemetryService.Alarms.MaxDeleteRetries;
            this.httpContextAccessor = contextAccessor;
            this.appConfigurationClient = appConfigurationClient;
            this.kustoQueryClient = kustoQueryClient;
            this.kustoEnabled = config.DeviceTelemetryService.Messages.TelemetryStorageType.Equals(
                TelemetryStorageTypeConstants.Ade, StringComparison.OrdinalIgnoreCase);
        }

        private string CollectionId
        {
            get
            {
                return this.appConfigurationClient.GetValue(
                    $"{TenantInfoKey}:{this.httpContextAccessor.HttpContext.Request.GetTenant()}:{AlarmsCollectionKey}");
            }
        }

        private string TenanId
        {
            get
            {
                return this.httpContextAccessor.HttpContext.Request.GetTenant();
            }
        }

        public async Task<Alarm> GetAsync(string id)
        {
            if (this.kustoEnabled)
            {
                return await this.GetAlertsADXByIdAsync(id);
            }

            Document doc = await this.GetDocumentByIdAsync(id);
            return new Alarm(doc);
        }

        public async Task<List<Alarm>> ListAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            if (this.kustoEnabled)
            {
                return await this.GetListAlarmsFromKustoAsync(from, to, order, skip, limit, devices);
            }

            return await this.GetListAlarmsFromCosmosDBAsync(from, to, order, skip, limit, devices);
        }

        public async Task<List<Alarm>> ListByRuleAsync(
            string id,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            if (this.kustoEnabled)
            {
                return await this.GetListAlarmsFromKustoAsync(from, to, order, skip, limit, devices, id);
            }

            return await this.GetListAlarmsFromCosmosDBAsync(from, to, order, skip, limit, devices, id);
        }

        public async Task<List<Alarm>> GetAllAlarmsListByRuleAsync(
            string[] rules,
            string order,
            int skip,
            int limit)
        {
            if (this.kustoEnabled)
            {
                return await this.GetAllAlarmsListByRuleKustoAsync(rules, order, skip, limit);
            }

            return await this.GetAllAlarmsListByRuleCosmosDBAsync(rules, order, skip, limit);
        }

        public async Task<int> GetCountByRuleAsync(
            string id,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string[] devices)
        {
            if (this.kustoEnabled)
            {
                return await this.GetCountByRuleKustoAsync(id, from, to, devices);
            }

            return await this.GetCountByRuleCosmosDBAsync(id, from, to, devices);
        }

        public async Task<Alarm> UpdateAsync(string id, string status)
        {
            InputValidator.Validate(id);
            InputValidator.Validate(status);

            if (this.kustoEnabled)
            {
                Alarm alarm = await this.GetAlertsADXByIdAsync(id);
                if (alarm != null)
                {
                    alarm.Status = status;

                    List<Alarm> alarms = new List<Alarm>();
                    alarms.Add(alarm);
                    await this.SendToEventHub(alarms);

                    return alarm;
                }
                else
                {
                    throw new ResourceNotFoundException($"No alarms exist.");
                }
            }

            Document document = await this.GetDocumentByIdAsync(id);
            document.SetPropertyValue(StatusKey, status);

            document = await this.storageClient.UpsertDocumentAsync(
                this.databaseName,
                this.CollectionId,
                document);

            return new Alarm(document);
        }

        public async Task Delete(List<string> ids)
        {
            if (this.kustoEnabled)
            {
                await this.DeleteKusto(ids);
                return;
            }

            await this.DeleteCosmosDB(ids);
        }

        /**
         * Delete an individual alarm by id. If the delete fails for a DocumentClientException
         * other than not found, retry up to this.maxRetryCount
         */
        public async Task DeleteAsync(string id)
        {
            if (this.kustoEnabled)
            {
                await this.DeleteKustoAsync(id);
                return;
            }

            await this.DeleteCosmosDBAsync(id);
        }

        private async Task<Document> GetDocumentByIdAsync(string id)
        {
            InputValidator.Validate(id);

            var query = new SqlQuerySpec(
                "SELECT * FROM c WHERE c.id=@id",
                new SqlParameterCollection(new SqlParameter[]
                {
                    new SqlParameter { Name = "@id", Value = id },
                }));

            // Retrieve the document using the DocumentClient.
            List<Document> documentList = await this.storageClient.QueryDocumentsAsync(
                this.databaseName,
                this.CollectionId,
                null,
                query,
                0,
                DocumentQueryLimit);

            if (documentList.Count > 0)
            {
                return documentList[0];
            }

            return null;
        }

        private async Task<List<Alarm>> GetListAlarmsFromCosmosDBAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices,
            string ruleId = null)
        {
            string ruleIdProperty = string.Empty;

            if (!string.IsNullOrEmpty(ruleId))
            {
                ruleIdProperty = RuleIdKey;
            }

            var sql = QueryBuilder.GetDocumentsSql(
                AlarmSchemaKey,
                ruleId,
                ruleIdProperty,
                from,
                MessageReceivedKey,
                to,
                MessageReceivedKey,
                order,
                MessageReceivedKey,
                skip,
                limit,
                devices,
                DeviceIdKey);

            this.logger.LogDebug("Created alarm query {sql}", sql);

            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                EnableScanInQuery = true,
            };

            try
            {
                List<Document> docs = await this.storageClient.QueryDocumentsAsync(
                    this.databaseName,
                    this.CollectionId,
                    queryOptions,
                    sql,
                    skip,
                    limit);

                return docs == null ?
                    new List<Alarm>() :
                    docs
                        .Select(doc => new Alarm(doc))
                        .ToList();
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No alarms exist in CosmosDb. The alarms collection {this.CollectionId} does not exist.", e);
            }
        }

        private async Task<List<Alarm>> GetListAlarmsFromKustoAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices,
            string ruleId = null)
        {
            string database = string.Format(IoTDatabaseNameFormat, this.TenanId);

            string ruleIdProperty = string.Empty;

            if (!string.IsNullOrEmpty(ruleId))
            {
                ruleIdProperty = RuleIdKustoKey;
            }

            (string query, Dictionary<string, string> queryParameter) = QueryBuilder.GetKustoQuery(
                AlertsKustoTableKey,
                ruleId,
                ruleIdProperty,
                from,
                DateModifiedKustoKey,
                to,
                DateModifiedKustoKey,
                order,
                DateModifiedKustoKey,
                skip,
                limit,
                devices,
                DeviceIdKustoKey,
                DateModifiedKustoKey);

            return await this.kustoQueryClient.ExecuteQueryAsync<Alarm>(database, query, queryParameter);
        }

        private async Task<Alarm> GetAlertsADXByIdAsync(string id)
        {
            InputValidator.Validate(id);
            string database = string.Format(IoTDatabaseNameFormat, this.TenanId);

            var query = "declare query_parameters ( idValue:string ); \n Alerts \n | summarize arg_max(DateModified, *) by Id | where IsDeleted == false \n | where Id == idValue";
            var queryParameter = new Dictionary<string, string>();
            queryParameter.Add("idValue", id);

            // Retrieve the document using the DocumentClient.
            var results = await this.kustoQueryClient.ExecuteQueryAsync<Alarm>(database, query, queryParameter);

            if (results.Count > 0)
            {
                return results.FirstOrDefault();
            }

            return null;
        }

        private async Task<List<Alarm>> GetAllAlarmsListByRuleCosmosDBAsync(
            string[] rules,
            string order,
            int skip,
            int limit)
        {
            var sql = QueryBuilder.GetDocumentsSql(
                AlarmSchemaKey,
                order,
                MessageReceivedKey,
                skip,
                limit,
                rules,
                RuleIdKey);

            this.logger.LogDebug("Created alarm by rule query {sql}", sql);

            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                EnableScanInQuery = true,
            };

            try
            {
                List<Document> docs = await this.storageClient.QueryDocumentsAsync(
                    this.databaseName,
                    this.CollectionId,
                    queryOptions,
                    sql,
                    skip,
                    limit);

                return docs == null ?
                    new List<Alarm>() :
                    docs
                        .Select(doc => new Alarm(doc))
                        .ToList();
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No alarms exist in CosmosDb. The alarms collection {this.CollectionId} does not exist.", e);
            }
        }

        private async Task<List<Alarm>> GetAllAlarmsListByRuleKustoAsync(
            string[] rules,
            string order,
            int skip,
            int limit)
        {
            string database = string.Format(IoTDatabaseNameFormat, this.TenanId);

            (string query, Dictionary<string, string> queryParameter) = QueryBuilder.GetKustoQuery(
                AlertsKustoTableKey,
                order,
                DateModifiedKustoKey,
                skip,
                limit,
                rules,
                RuleIdKustoKey,
                DateModifiedKustoKey);

            return await this.kustoQueryClient.ExecuteQueryAsync<Alarm>(database, query, queryParameter);
        }

        private async Task<int> GetCountByRuleCosmosDBAsync(
            string id,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string[] devices)
        {
            // build sql query to get open/acknowledged alarm count for rule
            string[] statusList = { AlarmStatusOpen, AlarmStatusAcknowledged };
            var sql = QueryBuilder.GetCountSql(
                AlarmSchemaKey,
                id,
                RuleIdKey,
                from,
                MessageReceivedKey,
                to,
                MessageReceivedKey,
                devices,
                DeviceIdKey,
                statusList,
                StatusKey);

            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                EnableScanInQuery = true,
            };

            try
            {
                return await this.storageClient.QueryCountAsync(
                    this.databaseName,
                    this.CollectionId,
                    queryOptions,
                    sql);
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No alarms exist in CosmosDb. The alarms collection {this.CollectionId} does not exist.", e);
            }
        }

        private async Task<int> GetCountByRuleKustoAsync(
            string id,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string[] devices)
        {
            // build sql query to get open/acknowledged alarm count for rule
            string[] statusList = { AlarmStatusOpen, AlarmStatusAcknowledged };
            string database = string.Format(IoTDatabaseNameFormat, this.TenanId);

            (string query, Dictionary<string, string> queryParameter) = QueryBuilder.GetCountKustoSql(
                AlertsKustoTableKey,
                id,
                RuleIdKustoKey,
                from,
                DateModifiedKustoKey,
                to,
                DateModifiedKustoKey,
                devices,
                DeviceIdKustoKey,
                statusList,
                StatusKustoKey,
                DateModifiedKustoKey);

            return await this.kustoQueryClient.ExecuteCountQueryAsync(database, query, queryParameter);
        }

        private async Task SendToEventHub(List<Alarm> alarms)
        {
            try
            {
                ConcurrentBag<Azure.Messaging.EventHubs.EventData> events = new ConcurrentBag<Azure.Messaging.EventHubs.EventData>();

                Parallel.ForEach(alarms, alarm =>
                {
                    JObject alarmMappingJson = new JObject();
                    alarmMappingJson.Add("id", alarm.Id);
                    alarmMappingJson.Add("created", alarm.DateCreated.ToUnixTimeMilliseconds());
                    alarmMappingJson.Add("modified", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    alarmMappingJson.Add("description", alarm.Description);
                    alarmMappingJson.Add("groupId", alarm.GroupId);
                    alarmMappingJson.Add("deviceId", alarm.DeviceId);
                    alarmMappingJson.Add("status", alarm.Status);
                    alarmMappingJson.Add("ruleId", alarm.RuleId);
                    alarmMappingJson.Add("ruleSeverity", alarm.RuleSeverity);
                    alarmMappingJson.Add("ruleDescription", alarm.RuleDescription);
                    alarmMappingJson.Add("isDeleted", alarm.IsDeleted);
                    var byteMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(alarmMappingJson));
                    var deviceMappingEventData = new Azure.Messaging.EventHubs.EventData(byteMessage);
                    events.Add(deviceMappingEventData);
                });

                try
                {
                    var eventHubConnString = this.appConfigurationClient.GetValue($"tenant:{this.TenanId}:eventHubConn");
                    EventHubHelper eventHubHelper = new EventHubHelper(eventHubConnString);

                    await eventHubHelper.SendMessageToEventHub($"{this.TenanId}-alerts", events.ToArray());
                }
                catch (Exception e)
                {
                    throw new Exception($"Unable to Send the alert data models to Entity Hub. TenantId: {this.TenanId}", e);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to Send the alert data models to Entity Hub. TenantId: {this.TenanId}", e);
            }
        }

        /**
         * Delete an individual alarm by id. If the delete fails for a DocumentClientException
         * other than not found, retry up to this.maxRetryCount
         */
        private async Task DeleteCosmosDBAsync(string id)
        {
            InputValidator.Validate(id);

            int retryCount = 0;
            while (retryCount < this.maxDeleteRetryCount)
            {
                try
                {
                    await this.storageClient.DeleteDocumentAsync(
                        this.databaseName,
                        this.CollectionId,
                        id);
                    return;
                }
                catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return;
                }
                catch (Exception e)
                {
                    // only delay if there is a suggested retry (i.e. if the request is throttled)
                    TimeSpan retryTimeSpan = TimeSpan.Zero;
                    if (e.GetType() == typeof(DocumentClientException))
                    {
                        retryTimeSpan = ((DocumentClientException)e).RetryAfter;
                    }

                    retryCount++;

                    if (retryCount >= this.maxDeleteRetryCount)
                    {
                        this.logger.LogError(e, "Failed to delete alarm {id}", id);
                        throw new ExternalDependencyException(e.Message);
                    }

                    this.logger.LogWarning(e, "Exception on delete alarm {id}", id);
                    Thread.Sleep(retryTimeSpan);
                }
            }
        }

        /**
         * Delete an individual alarm by id. If the delete fails for a DocumentClientException
         * other than not found, retry up to this.maxRetryCount
         */
        private async Task DeleteKustoAsync(string id)
        {
            InputValidator.Validate(id);

            int retryCount = 0;
            while (retryCount < this.maxDeleteRetryCount)
            {
                try
                {
                    Alarm alarm = await this.GetAlertsADXByIdAsync(id);
                    if (alarm != null)
                    {
                        alarm.IsDeleted = true;

                        List<Alarm> alarms = new List<Alarm>();
                        alarms.Add(alarm);
                        await this.SendToEventHub(alarms);
                    }

                    return;
                }
                catch (Exception e)
                {
                    // only delay if there is a suggested retry (i.e. if the request is throttled)
                    TimeSpan retryTimeSpan = TimeSpan.Zero;

                    retryCount++;

                    if (retryCount >= this.maxDeleteRetryCount)
                    {
                        this.logger.LogError(e, "Failed to delete alarm {id}", id);
                        throw new ExternalDependencyException(e.Message);
                    }

                    this.logger.LogWarning(e, "Exception on delete alarm {id}", id);
                    Thread.Sleep(retryTimeSpan);
                }
            }
        }

        private async Task DeleteCosmosDB(List<string> ids)
        {
            foreach (var id in ids)
            {
                InputValidator.Validate(id);
            }

            Task[] taskList = new Task[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                taskList[i] = this.DeleteCosmosDBAsync(ids[i]);
            }

            try
            {
                await Task.WhenAll(taskList);
            }
            catch (AggregateException aggregateException)
            {
                Exception inner = aggregateException.InnerExceptions[0];
                this.logger.LogError(inner, "Failed to delete alarm");
                throw inner;
            }
        }

        private async Task DeleteKusto(List<string> ids)
        {
            foreach (var id in ids)
            {
                InputValidator.Validate(id);
            }

            string database = string.Format(IoTDatabaseNameFormat, this.TenanId);

            (string query, Dictionary<string, string> queryParameter) = QueryBuilder.GetKustoQuery(
                AlertsKustoTableKey,
                ids.ToArray(),
                "Id",
                DateModifiedKustoKey);

            List<Alarm> alarms = await this.kustoQueryClient.ExecuteQueryAsync<Alarm>(database, query, queryParameter);

            if (alarms.Count > 0)
            {
                foreach (var alarm in alarms)
                {
                    alarm.IsDeleted = true;
                }

                await this.SendToEventHub(alarms);
            }
        }
    }
}