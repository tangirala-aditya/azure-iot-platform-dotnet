// <copyright file="DeviceTwinMirrorModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Shared;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceTwinMirrorModel
    {
        public DeviceTwinMirrorModel()
        {
        }

        public string DeviceId { get; set; }

        public Twin Twin { get; set; }

        public DateTime TimeStamp { get; set; }

        public DateTime DeviceCreatedDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}