// <copyright file="DeviceRegistryError.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceRegistryError
    {
        public DeviceRegistryError(DeviceRegistryOperationError[] errors)
        {
            foreach (var error in errors)
            {
                this.DeviceId = error.DeviceId;
                this.ModuleId = error.ModuleId;
                this.ErrorCode = error.ErrorCode;
                this.ErrorStatus = error.ErrorStatus;
            }
        }

        // The ID of the device that indicated the error.
        [JsonProperty(PropertyName = "deviceId", NullValueHandling = NullValueHandling.Ignore)]
        public string DeviceId
        {
            get;
            set;
        }

        // Summary:
        //     Module Id on the device that indicated the error.
        [JsonProperty(PropertyName = "moduleId", NullValueHandling = NullValueHandling.Ignore)]
        public string ModuleId
        {
            get;
            set;
        }

        // Summary:
        //     ErrorCode associated with the error.
        [JsonProperty(PropertyName = "errorCode", NullValueHandling = NullValueHandling.Ignore)]
        public ErrorCode ErrorCode
        {
            get;
            set;
        }

        // Summary:
        //     Additional details associated with the error.
        [JsonProperty(PropertyName = "errorStatus", NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorStatus
        {
            get;
            set;
        }
    }
}