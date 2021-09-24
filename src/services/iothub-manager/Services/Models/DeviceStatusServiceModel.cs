// <copyright file="DeviceStatusServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceStatusServiceModel : Audit
    {
        public DeviceStatusServiceModel()
        {
        }

        public string DeploymentId { get; set; }

        public IDictionary<string, DeploymentStatus> DeviceStatuses { get; set; }
    }
}