// <copyright file="DeviceGroupsConverter.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Mmm.Iot.AsaManager.Services.External.IotHubManager;
using Mmm.Iot.AsaManager.Services.Models;
using Mmm.Iot.AsaManager.Services.Models.DeviceGroups;
using Mmm.Iot.AsaManager.Services.Models.Storage;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.BlobStorage;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.External.TableStorage;
using Newtonsoft.Json;

namespace Mmm.Iot.AsaManager.Services
{
    public class DeviceGroupsConverter : Converter, IConverter
    {
        private const string CsvHeader = "DeviceId,GroupId";
        private readonly IIotHubManagerClient iotHubManager;
        private readonly ITableStorageClient tableStorageClient;

        public DeviceGroupsConverter(
            IIotHubManagerClient iotHubManager,
            IBlobStorageClient blobClient,
            IStorageAdapterClient storageAdapterClient,
            ITableStorageClient tableStorageClient,
            ILogger<DeviceGroupsConverter> log)
                : base(blobClient, storageAdapterClient, log)
        {
            this.iotHubManager = iotHubManager;
            this.tableStorageClient = tableStorageClient;
        }

        public override string Entity
        {
            get
            {
                return "devicegroups";
            }
        }

        public override string FileExtension
        {
            get
            {
                return "csv";
            }
        }

        public override async Task<ConversionApiModel> ConvertAsync(string tenantId, string operationId = null)
        {
            ValueListApiModel deviceGroups = null;
            try
            {
                deviceGroups = await this.StorageAdapterClient.GetAllAsync(this.Entity, tenantId);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Unable to query {entity} using storage adapter. OperationId: {operationId}. TenantId: {tenantId}", this.Entity, operationId, tenantId);
                throw e;
            }

            if (deviceGroups.Items.Count() == 0 || deviceGroups == null)
            {
                string errorMessage = $"No entities were receieved from storage adapter to convert to {this.Entity}. OperationId: {operationId}. TenantId: {tenantId}";
                this.Logger.LogError(new Exception(errorMessage), errorMessage);
                throw new ResourceNotFoundException("No entities were receieved from storage adapter to convert to rules.");
            }

            DeviceGroupListModel deviceGroupModels = new DeviceGroupListModel();
            try
            {
                List<DeviceGroupModel> items = new List<DeviceGroupModel>();
                foreach (ValueApiModel group in deviceGroups.Items)
                {
                    try
                    {
                        DeviceGroupDataModel dataModel = JsonConvert.DeserializeObject<DeviceGroupDataModel>(group.Data);
                        DeviceGroupModel individualModel = new DeviceGroupModel(group.Key, group.ETag, dataModel);
                        items.Add(individualModel);
                    }
                    catch (Exception)
                    {
                        this.Logger.LogInformation("Unable to convert a device group to the proper reference data model for {entity}. OperationId: {operationId}. TenantId: {tenantId}", this.Entity, operationId, tenantId);
                    }
                }

                if (items.Count() == 0)
                {
                    throw new ResourceNotSupportedException("No device groups were able to be converted to the proper rule reference data model.");
                }

                deviceGroupModels.Items = items;
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Unable to convert {entity} queried from storage adapter to appropriate data model. OperationId: {operationId}. TenantId: {tenantId}", this.Entity, operationId, tenantId);
                throw e;
            }

            Dictionary<DeviceGroupModel, DeviceListModel> deviceMapping = new Dictionary<DeviceGroupModel, DeviceListModel>();
            foreach (DeviceGroupModel deviceGroup in deviceGroupModels.Items)
            {
                try
                {
                    string cToken = string.Empty;
                    List<DeviceModel> completeDevicesList = new List<DeviceModel>();

                    do
                    {
                        DeviceListModel devicesList = await this.iotHubManager.GetListAsync(deviceGroup.Conditions, tenantId, cToken);
                        if (devicesList.Items.Count() > 0)
                        {
                            completeDevicesList.AddRange(devicesList.Items);
                        }

                        cToken = devicesList.ContinuationToken;
                    }
                    while (!string.IsNullOrEmpty(cToken));

                    if (completeDevicesList.Count > 0)
                    {
                        deviceMapping.Add(deviceGroup, new DeviceListModel()
                        {
                            Items = completeDevicesList,
                        });
                    }
                }
                catch (Exception e)
                {
                    // Do not throw an exception here, attempt to query other device groups instead to get as much data as possible
                    // Log all device groups that could not be retreived
                    this.Logger.LogError(e, "Unable to get list of devices for devicegroup {deviceGroup} from IotHubManager. OperationId: {operationId}. TenantId: {tenantId}", deviceGroup.Id, operationId, tenantId);
                }
            }

            if (deviceMapping.Count() == 0)
            {
                string groups = $"[{string.Join(", ", deviceGroupModels.Items.Select(group => group.Id))}]";
                string errorMessage = $"No Devices were found for any {this.Entity}. OperationId: {operationId}. TenantId: {tenantId}\n{groups}";
                this.Logger.LogError(new Exception(errorMessage), errorMessage);
                throw new ResourceNotFoundException($"No Devices were found for any {this.Entity}.");
            }

            string fileContent = null;
            try
            {
                // Write a file in csv format:
                // deviceId,groupId
                // mapping contains devices groups, and a list model of all devices within each device group
                // create a new csv row for each device and device group combination
                string fileContentRows = string.Join("\n", deviceMapping.Select(mapping =>
                {
                    return string.Join("\n", mapping.Value.Items.Select(device => $"{device.Id},{mapping.Key.Id}"));
                }));

                // Add the rows and the header together to complete the csv file content
                fileContent = $"{CsvHeader}\n{fileContentRows}";
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Unable to serialize the {entity} data models for the temporary file content. OperationId: {operationId}. TenantId: {tenantId}", this.Entity, operationId, tenantId);
                throw e;
            }

            this.WriteDataToTableStorageAndQueue(deviceMapping, tenantId);

            // string blobFilePath = await this.WriteFileContentToBlobAsync(fileContent, tenantId, operationId);
            ConversionApiModel conversionResponse = new ConversionApiModel
            {
                TenantId = tenantId,
                BlobFilePath = string.Empty,
                Entities = deviceGroups,
                OperationId = operationId,
            };
            this.Logger.LogInformation("Successfully Completed {entity} conversion\n{model}", this.Entity, JsonConvert.SerializeObject(conversionResponse));
            return conversionResponse;
        }

        private async void WriteDataToTableStorageAndQueue(Dictionary<DeviceGroupModel, DeviceListModel> deviceMappings, string tenantId)
        {
            if (deviceMappings != null && deviceMappings.Keys.Count > 0)
            {
                List<DeviceGroupMapping> deviceGroupMappings = await this.tableStorageClient.QueryAsync($"devicegroupdevices{tenantId.Replace("-", string.Empty)}", new TableQuery<DeviceGroupMapping>());

                if (deviceGroupMappings.Count > 0)
                {
                    var groupedDeviceGroupMapping = deviceGroupMappings.GroupBy(d => d.DeviceGroupId);

                    foreach (var deviceGroupMapping in deviceMappings)
                    {
                        var existingDeviceGroupMappings = groupedDeviceGroupMapping.FirstOrDefault(g => g.Key == deviceGroupMapping.Key.Id);

                        if (existingDeviceGroupMappings != null && existingDeviceGroupMappings.Any())
                        {
                            var devices = existingDeviceGroupMappings.ToList();

                            IEnumerable<string> deviceIds = devices.Select(x => x.DeviceId);

                            var newDeviceIds = deviceGroupMapping.Value.Items.Select(x => x.Id).Except(deviceIds);

                            if (newDeviceIds != null && newDeviceIds.Count() > 0)
                            {
                                foreach (var newDeviceId in newDeviceIds)
                                {
                                    DeviceGroupMapping newdeviceGroupMapping = new DeviceGroupMapping(existingDeviceGroupMappings.Key, newDeviceId);
                                    await this.tableStorageClient.InsertAsync($"devicegroupdevices{tenantId.Replace("-", string.Empty)}", newdeviceGroupMapping);
                                }

                                IEnumerable<List<string>> batchedData = this.SplitDevicesIntoBatches(newDeviceIds.ToList());
                                if (batchedData != null && batchedData.Count() > 0)
                                {
                                    QueueClient queueClient = new QueueClient("DefaultEndpointsProtocol=https;AccountName=teststorageacsragav;AccountKey=H8b0TZ5zcHayHUtV0B3sQXVa8IQVEyRQDqWPIqrceW/sA+DbcpvBdRoSfCkaXc0fU2GIBm8wtvMJENLKK3YEqQ==;EndpointSuffix=core.windows.net", "deploymentRequests", new QueueClientOptions() { MessageEncoding = QueueMessageEncoding.Base64 });
                                    await queueClient.CreateIfNotExistsAsync();

                                    foreach (var data in batchedData)
                                    {
                                        // Insert into Queue
                                        DeploymentRequest deploymentRequest = new DeploymentRequest
                                        {
                                            TenantId = tenantId,
                                            DeviceGroupId = existingDeviceGroupMappings.Key,
                                            Devices = data,
                                        };

                                        if (queueClient.Exists())
                                        {
                                            try
                                            {
                                                queueClient.SendMessage(JsonConvert.SerializeObject(deploymentRequest));
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    List<DeviceGroupMapping> deviceGroupMappingsToInsert = new List<DeviceGroupMapping>();
                    foreach (var deviceMapping in deviceMappings)
                    {
                        if (deviceMapping.Value.Items.Count() > 0)
                        {
                            foreach (var item in deviceMapping.Value.Items)
                            {
                                DeviceGroupMapping deviceGroupMapping = new DeviceGroupMapping(deviceMapping.Key.Id, item.Id);
                                deviceGroupMappingsToInsert.Add(deviceGroupMapping);
                            }
                        }
                    }

                    if (deviceGroupMappingsToInsert.Count > 0)
                    {
                        foreach (var deviceGroupMappingToInsert in deviceGroupMappingsToInsert)
                        {
                            await this.tableStorageClient.InsertAsync($"devicegroupdevices{tenantId.Replace("-", string.Empty)}", deviceGroupMappingToInsert);
                        }
                    }
                }
            }
        }

        private IEnumerable<List<string>> SplitDevicesIntoBatches(List<string> devices, int batchSize = 250)
        {
            for (int i = 0; i < devices.Count; i += batchSize)
            {
                yield return devices.GetRange(i, Math.Min(batchSize, devices.Count - i));
            }
        }
    }
}