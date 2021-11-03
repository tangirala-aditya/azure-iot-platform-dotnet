// <copyright file="IDevices.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services;
using Mmm.Iot.IoTHubManager.Services.Models;

namespace Mmm.Iot.IoTHubManager.Services
{
    public interface IDevices : IStatusOperation
    {
        Task<DeviceServiceListModel> GetListAsync(string query, string continuationToken);

        Task<DeviceServiceListModel> GetListFromIoTHubAsync(string inputQuery, string continuationToken);

        Task<DeviceServiceListModel> GetListFromADXAsync(string query);

        Task<DeviceStatisticsServiceModel> GetDeviceStatisticsAsync(string query);

        Task<DeviceTwinName> GetDeviceTwinNamesAsync();

        Task<DeviceServiceModel> GetAsync(string id);

        Task<DeviceServiceModel> CreateAsync(DeviceServiceModel toServiceModel);

        Task<DeviceServiceModel> UpdateAsync(DeviceServiceModel toServiceModel, DevicePropertyDelegate devicePropertyDelegate);

        Task DeleteAsync(string id);

        Task<TwinServiceModel> GetModuleTwinAsync(string deviceId, string moduleId);

        Task<TwinServiceListModel> GetModuleTwinsByQueryAsync(string query, string continuationToken);

        Task<DeploymentHistoryListModel> GetDeploymentHistoryAsync(string deviceId, string tenantId);

        Task<DeviceServiceListModel> GetDeviceListAsync(string query, string continuationToken);

        Task<List<DeviceReportServiceModel>> GetDeviceListForReport(string query);

        Task<bool> LinkToGateway(string deviceId, string edgeDeviceId);

        Task<DeviceServiceListModel> GetChildDevices(string edgeDeviceId);

        Task<BulkOperationResult> LinkDevicesToGateway(IEnumerable<string> deviceIds, string parentDeviceId, string tenantId, string userId);

        Task<BulkOperationResult> LinkDeviceGroupToGateway(string deviceGroupId, string parentDeviceId, string tenantId, string userId);

        Task<DeviceLinkingJobServiceListModel> GetDeviceLinkingJobs(string tenantId);

        Task<DeviceLinkingJobServiceListModel> GetDeviceLinkingJobsByJobId(string jobId, string tenantId);

        Task<BulkOperationResult> UnlinkFromGateway(List<string> deviceIds);

        Task<ModuleServiceListModel> GetModuleTwinsByDeviceIdAsync(string deviceId, string continuationToken = null);
    }
}