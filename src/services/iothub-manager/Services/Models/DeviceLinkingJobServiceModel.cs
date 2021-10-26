// <copyright file="DeviceLinkingJobServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceLinkingJobServiceModel : Audit
    {
        public string JobId { get; set; }

        public SourceCategory Category { get; set; }

        public string ParentDeviceId { get; set; }

        public string DeviceGroupId { get; set; }

        public IEnumerable<string> DeviceIds { get; set; }

        public string JobStatus { get; set; }
    }
}