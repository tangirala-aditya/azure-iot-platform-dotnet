// <copyright file="DeviceGroupMapping.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Cosmos.Table;

namespace Mmm.Iot.AsaManager.Services.Models.Storage
{
    public class DeviceGroupMapping : TableEntity
    {
        public DeviceGroupMapping()
        {
        }

        public DeviceGroupMapping(string deviceGroupId, string deviceId)
        {
            this.PartitionKey = deviceGroupId;
            this.RowKey = deviceId;
            this.DeviceGroupId = deviceGroupId;
            this.DeviceId = deviceId;
        }

        public string DeviceGroupId { get; set; }

        public string DeviceId { get; set; }
    }
}