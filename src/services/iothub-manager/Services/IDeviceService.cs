// <copyright file="IDeviceService.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Mmm.Iot.IoTHubManager.Services.Models;

namespace Mmm.Iot.IoTHubManager.Services
{
    public interface IDeviceService
    {
        Task<MethodResultServiceModel> InvokeDeviceMethodAsync(string deviceId, MethodParameterServiceModel parameter);

        Task SendCloudToDeviceMessage(string deviceId, string message);

        Task<MethodResultServiceModel> InvokeDeviceMethodAsync(string deviceId, string moduleId, MethodParameterServiceModel parameter, bool tobeRestarted = false);

        Task<MethodResultServiceModel> PingModuleAsync(string deviceId);
    }
}