// <copyright file="DeviceGroupMappingRequest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.DigitalTwin.Models
{
    public class DeviceGroupMappingRequest
    {
        public string TenantId { get; set; }

        public string DeviceGroupId { get; set; }

        public List<string> Devices { get; set; }
    }
}