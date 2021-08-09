// <copyright file="DeviceService.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.Messaging.Shared.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.Messaging.Shared
{
    public class DeviceService
    {
        private bool success = true;

        public async Task SaveDeviceLifeCycleOperationAsync(string eventData, string tenant, string deviceId, string operationType)
        {
            try
            {
                string cosmosDbcollection = $"lifecycle-{tenant}";
                int cosmosCollRus = Convert.ToInt32(Environment.GetEnvironmentVariable("CosmosDBRus", EnvironmentVariableTarget.Process));
                string cosmosDatabase = Environment.GetEnvironmentVariable("DeviceStreamDatabaseId", EnvironmentVariableTarget.Process);

                CosmosOperations docClient = await CosmosOperations.GetClientAsync();
                bool updateStatus = await docClient.CreateCollectionIfNotExistsAsync(cosmosDatabase, cosmosDbcollection, cosmosCollRus, CosmosOperation.Device);

                if (updateStatus)
                {
                    if (operationType.Equals("createDeviceIdentity"))
                    {
                        JObject deviceTwinJson = JObject.Parse(eventData);
                        deviceTwinJson.Add(DeviceTelemetryKeyConstants.Id, deviceId);
                        docClient = await CosmosOperations.GetClientAsync();
                        await docClient.SaveDocumentAsync(deviceTwinJson, this.GenerateCollectionLink(cosmosDatabase, cosmosDbcollection));
                    }
                    else
                    {
                        docClient = await CosmosOperations.GetClientAsync();
                        await docClient.DeleteDocumentAsync(deviceId, this.GenerateCollectionLink(cosmosDatabase, cosmosDbcollection));
                    }
                }
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Save Device Lifecycle operation failed: {exception}, operation type: {operationType}, tenant: {tenant}, deviceId {deviceId}");
            }
        }

        public async Task SaveTelemetryToCosmosAsync(string telemetryMessage, string tenant)
        {
            try
            {
                string cosmosDbcollection = $"telemetry-{tenant}";
                int cosmosCollRus = Convert.ToInt32(Environment.GetEnvironmentVariable("CosmosDBRus", EnvironmentVariableTarget.Process));
                string cosmosDatabase = Environment.GetEnvironmentVariable("DeviceStreamDatabaseId", EnvironmentVariableTarget.Process);

                CosmosOperations docClient = await CosmosOperations.GetClientAsync();
                bool updateStatus = await docClient.CreateCollectionIfNotExistsAsync(cosmosDatabase, cosmosDbcollection, cosmosCollRus, CosmosOperation.Device);
                if (updateStatus)
                {
                    await docClient.SaveDocumentAsync(JsonConvert.DeserializeObject(telemetryMessage), this.GenerateCollectionLink(cosmosDatabase, cosmosDbcollection));
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Save Telemetry message failed", ex);
            }
        }

        public async Task SaveDeviceTwinChangeAsync(string eventData, string tenant, string deviceId, double timeReceived)
        {
            try
            {
                string cosmosDbcollection = $"twin-change-{tenant}";
                int cosmosCollRus = Convert.ToInt32(Environment.GetEnvironmentVariable("CosmosDBRus", EnvironmentVariableTarget.Process));
                string cosmosDatabase = Environment.GetEnvironmentVariable("DeviceStreamDatabaseId", EnvironmentVariableTarget.Process);

                CosmosOperations docClient = await CosmosOperations.GetClientAsync();
                bool updateStatus = await docClient.CreateCollectionIfNotExistsAsync(cosmosDatabase, cosmosDbcollection, cosmosCollRus, CosmosOperation.Device);
                if (updateStatus)
                {
                    JObject deviceTwinJson = JObject.Parse(eventData);
                    deviceTwinJson.Add(DeviceTelemetryKeyConstants.Id, deviceId);
                    deviceTwinJson.Add(DeviceTelemetryKeyConstants.DeviceId, deviceId);
                    deviceTwinJson.Add(DeviceTelemetryKeyConstants.TimeReceived, timeReceived);
                    deviceTwinJson.Add(DeviceTelemetryKeyConstants.Schema, DeviceTelemetryKeyConstants.TwinChangeSchema);

                    await docClient.SaveDocumentAsync(deviceTwinJson, this.GenerateCollectionLink(cosmosDatabase, cosmosDbcollection));
                }
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Save Device Twin failed: {exception},  tenant: {tenant}, deviceId: {deviceId}");
            }
        }

        public async Task ProcessTelemetryAsync(EventData[] source, ILogger log, int batchThreshold = 12, int batchWriteDelay = 85)
        {
            if (batchThreshold <= 0)
            {
                throw new ArgumentOutOfRangeException("batchThreshold must be greater than 0");
            }

            List<Task> messagingTasks = new List<Task>();

            // iterate over the eventData array and add a task to process each element
            foreach (EventData eventData in source)
            {
                // get the iothub-enqueuedtime, iothub-connection-device-id, and tenent from the eventData system properties
                // these values are to be added to the telemetry message before being stored in cosmos (tenent is used to save telemetry to cosmos)
                eventData.SystemProperties.TryGetValue(DeviceTelemetryKeyConstants.IotHubEnqueuedTime, out object dateTimeReceived);
                eventData.SystemProperties.TryGetValue(DeviceTelemetryKeyConstants.IotHubConnectionDeviceId, out object deviceId);
                eventData.Properties.TryGetValue(DeviceTelemetryKeyConstants.Tenant, out object tenant);
                if (tenant == null || dateTimeReceived == null || deviceId == null)
                {
                    this.success = false;
                    log.LogError($"EventData did not contain one or more of the following: {DeviceTelemetryKeyConstants.Tenant}, {DeviceTelemetryKeyConstants.IotHubEnqueuedTime}, {DeviceTelemetryKeyConstants.IotHubEnqueuedTime}.");
                    continue;  // go on to the next event data - this one cannot be written
                }

                var telemetryTimestamp = new TelemetryTimestamp(Convert.ToDateTime(dateTimeReceived));

                bool isBatchedTelemetry = false;
                if (eventData.Properties.TryGetValue(DeviceTelemetryKeyConstants.BatchedTelemetry, out object isBatchedValue))
                {
                    bool.TryParse(isBatchedValue.ToString(), out isBatchedTelemetry);
                }

                try
                {
                    if (isBatchedTelemetry)
                    {
                        messagingTasks.Add(
                            Task.Run(async () =>
                            {
                                var batchedEventData = this.ConvertBatchedDataToKeyValueData(eventData, log);
                                await this.ProcessBatchedTelemetryMessagesAsync(
                                    batchedEventData,
                                    log,
                                    tenant,
                                    deviceId,
                                    telemetryTimestamp,
                                    batchThreshold,
                                    batchWriteDelay);
                            }));
                    }
                    else
                    {
                        messagingTasks.Add(Task.Run(async () =>
                        {
                            var telemetry = this.ConvertEventDataToJObject(eventData, log);
                            await this.ProcessTelemetryMessageAndWriteToCosmosAsync(
                                telemetry,
                                log,
                                tenant,
                                deviceId,
                                telemetryTimestamp);
                        }));
                    }
                }
                catch (Exception)
                {
                    this.success = false;
                    log.LogError($"An error occurred while writing some eventData to cosmos");
                }
            }

            await Task.WhenAll(messagingTasks.ToArray());
            if (!this.success)
            {
                throw new Exception("Failed to process one or more telemetry messages.");
            }
        }

        public async Task ProcessDeviceTwin(EventData[] source, ILogger log, string tenant, string deviceId)
        {
            foreach (EventData message in source)
            {
                try
                {
                    if (tenant != null)
                    {
                        var connectionString = TenantConnectionHelper.GetEventHubConnectionString(Convert.ToString(tenant));
                        EventHubHelper eventHubHelper = new EventHubHelper(connectionString);

                        string eventData = Encoding.UTF8.GetString(message.Body.Array);
                        message.Properties.TryGetValue("opType", out object operationType);
                        message.SystemProperties.TryGetValue(DeviceTelemetryKeyConstants.IotHubEnqueuedTime, out object dateTimeReceived);

                        var timeStamp = new TelemetryTimestamp(Convert.ToDateTime(dateTimeReceived));
                        DeviceService deviceService = new DeviceService();
                        await deviceService.SaveDeviceTwinOperationAsync(eventData, Convert.ToString(tenant), deviceId.ToString(), operationType.ToString(), timeStamp, eventHubHelper);
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"Error occurrred in for loop: {ex.Message} StackTrace: {ex.StackTrace}  Inner Exception: {(string.IsNullOrEmpty(ex.StackTrace) ? string.Empty : ex.StackTrace)}");
                }
            }
        }

        public async Task SaveDeviceTwinOperationAsync(string eventData, string tenant, string deviceId, string operationType, TelemetryTimestamp timeStamp, EventHubHelper eventHubHelper)
        {
            try
            {
                string deviceTwin = null;
                Twin twin = null;
                JObject deviceTwinJson = new JObject();
                deviceTwinJson.Add(DeviceTelemetryKeyConstants.DeviceId, deviceId.ToString());
                deviceTwinJson.Add(DeviceTelemetryKeyConstants.TimeStamp, timeStamp.DateTime);
                deviceTwinJson.Add(DeviceTelemetryKeyConstants.TimeReceived, timeStamp.EpochTimestamp);
                deviceTwinJson.Add(DeviceTelemetryKeyConstants.EventOpType, operationType);
                deviceTwinJson.Add(DeviceTelemetryKeyConstants.IsDeleted, false);

                if (operationType.ToString().Equals("createDeviceIdentity"))
                {
                    try
                    {
                        twin = await TenantConnectionHelper.GetRegistry(Convert.ToString(tenant)).GetTwinAsync(deviceId.ToString());
                        deviceTwin = twin.ToJson();
                        deviceTwinJson.Add(DeviceTelemetryKeyConstants.DeviceCreatedDate, timeStamp.DateTime);
                    }
                    catch (Exception)
                    {
                        deviceTwin = eventData;
                    }
                }
                else
                {
                    JObject previousTwin = null;
                    KustoOperations kustoClient = await KustoOperations.GetClientAsync();

                    string kustoQuery = $"DeviceTwin | where DeviceId == \"{deviceId}\" | summarize arg_max(TimeStamp, *) by DeviceId | where IsDeleted == false";
                    var deviceTwinList = await kustoClient.QueryAsync<DeviceTwinModel>($"IoT-{tenant}", kustoQuery, null);

                    DeviceTwinModel preDeviceTwin = deviceTwinList.FirstOrDefault();

                    if (preDeviceTwin != null)
                    {
                        previousTwin = preDeviceTwin.Twin;
                        deviceTwinJson.Add(DeviceTelemetryKeyConstants.DeviceCreatedDate, preDeviceTwin.DeviceCreatedDate);
                    }
                    else
                    {
                        deviceTwinJson.Add(DeviceTelemetryKeyConstants.DeviceCreatedDate, default(DateTime)); // Set Device Created Date to Default if twin is not present in storage.

                        twin = await TenantConnectionHelper.GetRegistry(Convert.ToString(tenant)).GetTwinAsync(deviceId.ToString());

                        if (twin != null)
                        {
                            previousTwin = JObject.Parse(twin.ToJson());
                        }
                    }

                    switch (operationType)
                    {
                        case "deviceConnected":
                            if (preDeviceTwin != null)
                            {
                                previousTwin["connectionState"] = "Connected";
                                previousTwin["lastActivityTime"] = timeStamp.DateTime;
                            }

                            break;
                        case "deviceDisconnected":
                            if (preDeviceTwin != null)
                            {
                                previousTwin["connectionState"] = "Disconnected";
                                previousTwin["lastActivityTime"] = timeStamp.DateTime;
                            }

                            break;
                        case "updateTwin":
                            if (preDeviceTwin != null)
                            {
                                JObject twinFragment = JObject.Parse(eventData);
                                previousTwin = previousTwin.UpdateJson(twinFragment);
                            }
                            else
                            {
                                previousTwin = JObject.Parse(eventData);
                            }

                            break;
                        case "deleteDeviceIdentity":
                            deviceTwinJson[DeviceTelemetryKeyConstants.IsDeleted] = true;
                            break;
                        default:
                            break;
                    }

                    deviceTwin = previousTwin?.ToString();
                }

                deviceTwinJson.Add(DeviceTelemetryKeyConstants.Data, deviceTwin);

                // Save the Device Twin Data to EventHub for further processing
                var byteMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceTwinJson));
                var eventDeviceTwinData = new Azure.Messaging.EventHubs.EventData(byteMessage);
                eventDeviceTwinData.Properties.Add("deviceid", deviceId.ToString());

                await eventHubHelper.SendMessageToEventHub($"{tenant}-devicetwin", new Azure.Messaging.EventHubs.EventData[] { eventDeviceTwinData });
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Save Device Twin operation failed: {exception}, operation type: {operationType}, tenant: {tenant}, deviceId {deviceId}");
            }
        }

        private string GenerateCollectionLink(string cosmosDatabase, string cosmosCollection)
        {
            return $"/dbs/{cosmosDatabase}/colls/{cosmosCollection}";
        }

        private async Task ProcessTelemetryMessageAndWriteToCosmosAsync(
            JObject telemetry,
            ILogger log,
            object tenant,
            object deviceId,
            TelemetryTimestamp telemetryTimestamp)
        {
            try
            {
                if (telemetry == null || !telemetry.HasValues)
                {
                    throw new Exception("Telemetry message had no values to write to cosmos");
                }

                telemetry[DeviceTelemetryKeyConstants.DeviceId] = deviceId.ToString();

                telemetry.Add(DeviceTelemetryKeyConstants.DateTimeReceived, telemetryTimestamp.DateTime.ToString());
                telemetry.Add(DeviceTelemetryKeyConstants.TimeReceived, telemetryTimestamp.EpochTimestamp);

                telemetry.Add(DeviceTelemetryKeyConstants.Schema, DeviceTelemetryKeyConstants.MessageSchema);

                // Save the telemetry message to Cosmos
                await this.SaveTelemetryToCosmosAsync(telemetry.ToString(), tenant.ToString());
            }
            catch (Exception ex)
            {
                this.success = false;
                log.LogError($"Error occurrred : {ex.Message}");
            }
        }

        private async Task ProcessBatchedTelemetryMessagesAsync(
            EventData[] batchedSource,
            ILogger log,
            object tenant,
            object deviceId,
            TelemetryTimestamp telemetryTimestamp,
            int batchThreshold,
            int batchWriteDelay)
        {
            // get the final data point for timestamp calculation
            // if batchedSource is empty set to null, as it's value will never be used.
            var finalDataPoint = batchedSource.Length > 0 ? batchedSource.Last() : null;
            var finalTelemetry = finalDataPoint != null ? this.ConvertEventDataToJObject(finalDataPoint, log) : null;
            var finalTimestamp = this.GetBatchedDataPointTimestamp(finalTelemetry, telemetryTimestamp);

            await Task.WhenAll(
                batchedSource.Select((dataPoint, idx) =>
                {
                    return Task.Run(async () =>
                    {
                        // apply some delay as the batch size grows
                        await Task.Delay(idx / batchThreshold * batchWriteDelay);

                        var telemetry = this.ConvertEventDataToJObject(dataPoint, log);
                        var dataPointTimestamp = this.GetBatchedDataPointTimestamp(
                            telemetry,
                            telemetryTimestamp);
                        var computedOffsetTimestamp = this.ComputeBatchedEnqueuedTime(
                            telemetryTimestamp,
                            dataPointTimestamp,
                            finalTimestamp);

                        await this.ProcessTelemetryMessageAndWriteToCosmosAsync(
                            telemetry,
                            log,
                            tenant,
                            deviceId,
                            computedOffsetTimestamp);
                    });
                }));
        }

        private JObject ConvertEventDataToJObject(EventData eventData, ILogger log)
        {
            string message = Encoding.UTF8.GetString(eventData.Body.Array);
            if (string.IsNullOrEmpty(message))
            {
                string errorMessage = "EventData contained an empty message body, no telemetry data could be written to CosmosDb for this message";
                log.LogError(errorMessage);
                throw new Exception(errorMessage);
            }

            return JsonConvert.DeserializeObject<JObject>(message);
        }

        private EventData[] ConvertBatchedDataToKeyValueData(EventData eventData, ILogger log, string metricKey = "channels", string dataKey = "data")
        {
            JToken batchedDataMetrics;
            JToken batchedDataPoints;
            try
            {
                JObject batchedMessage = this.ConvertEventDataToJObject(eventData, log);
                batchedDataMetrics = batchedMessage.GetValue(metricKey);
                batchedDataPoints = batchedMessage.GetValue(dataKey);
            }
            catch (Exception e)
            {
                this.success = false;
                log.LogError($"Batched data did not contain the {metricKey} or {dataKey} fields for unpacking data points.", e);
                return new EventData[0];
            }

            // unique identifier for identifying messages that failed to convert
            string failureId = Guid.NewGuid().ToString();

            try
            {
                return batchedDataPoints
                    .Select(dataPoint =>
                        {
                            try
                            {
                                Dictionary<string, object> telemetryData = batchedDataMetrics.Values<string>()
                                    .Zip(
                                        dataPoint.Values<object>(),
                                        (key, value) => new { key, value })
                                    .ToDictionary(x => x.key, x => x.value);
                                return JObject.FromObject(telemetryData);
                            }
                            catch (Exception e)
                            {
                                this.success = false;
                                log.LogError($"Unable to convert a single data point in a batched message. {dataPoint}", e);
                                return JObject.FromObject(new { unableToConvert = failureId });
                            }
                        })
                    .Where(jsonMessage =>
                        {
                            // check if the unableToConvert key was added to the message above
                            // continue if the key does not exist or is not set to our unique value
                            bool unableToConvertExists = jsonMessage.TryGetValue("unableToConvert", out JToken value);
                            return !unableToConvertExists || value.Value<string>() != failureId;
                        })
                    .Select(jsonMessage =>
                        {
                            var byteMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonMessage));
                            return new EventData(byteMessage);
                        })
                    .ToArray();
            }
            catch (Exception e)
            {
                this.success = false;
                log.LogError($"Unable to convert the batched {metricKey} and {dataKey} to single key/value event data.", e);
                return new EventData[0];
            }
        }

        private TelemetryTimestamp ComputeBatchedEnqueuedTime(
            TelemetryTimestamp reportedTimestamp,
            TelemetryTimestamp currentTimestamp,
            TelemetryTimestamp finalTimestamp)
        {
            // compute the delay from the final timestamp vs the iothub reported timestamp (IotHubEnqueuedTime)
            long offset = reportedTimestamp.EpochTimestamp - finalTimestamp.EpochTimestamp;

            // the computed time stamp takes the expected delay from the reportedTimestamp plus the actual timestamp from data
            return new TelemetryTimestamp(
                currentTimestamp.EpochTimestamp + offset,
                currentTimestamp.EpochTimestampMeasurement);
        }

        private TelemetryTimestamp GetBatchedDataPointTimestamp(JObject dataPoint, TelemetryTimestamp defaultTimestamp)
        {
            bool dataPointHasTimestamp = dataPoint.TryGetValue(
                DeviceTelemetryKeyConstants.BatchedDataPointTimestampKey,
                StringComparison.InvariantCultureIgnoreCase,
                out JToken dataPointTimestampToken);
            bool dataPointHasTimestampMeasurement = dataPoint.TryGetValue(
                DeviceTelemetryKeyConstants.BatchedDataPointTimestampMeasurementKey,
                StringComparison.InvariantCultureIgnoreCase,
                out JToken dataPointTimestampMeasurementToken);

            // take the value given in the dataPoint, otherwise default to seconds if the dataPoint has a value
            var timestampMeasurement = dataPointHasTimestampMeasurement
                ? dataPointTimestampMeasurementToken.Value<TimestampMeasurement>()
                : dataPointHasTimestamp
                    ? TimestampMeasurement.S
                    : defaultTimestamp.EpochTimestampMeasurement;

            return dataPointHasTimestamp
                ? new TelemetryTimestamp(
                    dataPointTimestampToken?.Value<long>() ?? defaultTimestamp.EpochTimestamp,
                    timestampMeasurement)
                : defaultTimestamp;
        }
    }
}