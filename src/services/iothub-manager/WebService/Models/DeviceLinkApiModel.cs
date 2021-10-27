// <copyright file="DeviceLinkApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class DeviceLinkApiModel
    {
        public List<string> DeviceIds { get; set; }

        public string ParentDeviceId { get; set; }

        public string DeviceGroupId { get; set; }
    }
}