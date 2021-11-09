// <copyright file="DeviceService.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Devices;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.IoTHubManager.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services.Models;

using Newtonsoft.Json.Linq;

namespace Mmm.Iot.IoTHubManager.Services
{
    public class DeviceService : IDeviceService
    {
        private ServiceClient serviceClient;

        public DeviceService(ITenantConnectionHelper tenantConnectionHelper)
        {
            string iotHubConnectionString = string.Empty;
            try
            {
                iotHubConnectionString = tenantConnectionHelper.GetIotHubConnectionString();
            }
            catch (Autofac.Core.DependencyResolutionException re)
            {
                throw new Exception("Unable to get the IotHub Connection String from the tenantConnectionHelper. This may be caused by a missing tenant id in the request, or a misconfigured App Configuration.", re);
            }

            if (string.IsNullOrEmpty(iotHubConnectionString))
            {
                throw new Exception("The IotHubConnectionString returned by App Config was null or empty.");
            }

            IoTHubConnectionHelper.CreateUsingHubConnectionString(
                iotHubConnectionString,
                conn => { this.serviceClient = ServiceClient.CreateFromConnectionString(conn); });
        }

        public async Task<MethodResultServiceModel> InvokeDeviceMethodAsync(string deviceId, MethodParameterServiceModel parameter)
        {
            var result = await this.serviceClient.InvokeDeviceMethodAsync(deviceId, parameter.ToAzureModel());
            return new MethodResultServiceModel(result);
        }

        public async Task SendCloudToDeviceMessage(string deviceId, string message)
        {
            await this.serviceClient.SendAsync(deviceId, new Message(Encoding.ASCII.GetBytes(message)));
        }

        public async Task<MethodResultServiceModel> InvokeDeviceMethodAsync(string deviceId, string moduleId, MethodParameterServiceModel parameterServiceModel)
        {
            CloudToDeviceMethod cloudToDeviceMethod = new CloudToDeviceMethod("GetModuleLogs");

            // string payload = "{'schemaVersion': '1.0','items': [{'id': {0},'filter': {'tail': 10}}],'encoding': 'none','contentType': 'json'}";
            // string formattedPayload = string.Format(payload, formattedModuleId);
            // JObject jobject = JObject.Parse(formattedPayload);
            string formattedModuleId = (moduleId == "$edgeAgent" || moduleId == "$edgeHub") ? moduleId.Substring(1) : moduleId;
            JObject itemsObject = new JObject
            {
                ["id"] = formattedModuleId,
                ["filter"] = new JObject { ["tail"] = 100 },
            };
            JObject jobject1 = new JObject
            {
                ["schemaVersion"] = "1.0",
                ["id"] = "echo",
                ["items"] = itemsObject,
                ["encoding"] = "none",
                ["contentType"] = "json",
            };
            cloudToDeviceMethod.SetPayloadJson(jobject1.ToString());
            var result = await this.serviceClient.InvokeDeviceMethodAsync(deviceId, "$edgeAgent", cloudToDeviceMethod);

            // var result = await this.serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, parameterServiceModel.ToAzureModel());
            return new MethodResultServiceModel(result);
        }
    }
}