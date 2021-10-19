// <copyright file="BulkOperationResult.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class BulkOperationResult
    {
        public BulkOperationResult(BulkRegistryOperationResult bulkRegistryOperationResult)
        {
            this.IsSuccessful = bulkRegistryOperationResult.IsSuccessful;
            this.Errors = bulkRegistryOperationResult.Errors;
        }

        // Summary:
        //     Whether or not the operation was successful.
        [JsonProperty(PropertyName = "isSuccessful", Required = Required.Always)]
        public bool IsSuccessful
        {
            get;
            set;
        }

        // Summary:
        //     If the operation was not successful, this contains an array of DeviceRegistryOperationError
        //     objects.
        [JsonProperty(PropertyName = "errors", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DeviceRegistryOperationError[] Errors
        {
            get;
            set;
        }
    }
}