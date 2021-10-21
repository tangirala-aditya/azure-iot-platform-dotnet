// <copyright file="DeviceGroup.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace MigrateIoTDeviceTwinToADX.Models
{
    public class DeviceGroup
    {
        public string Id { get; set; }

        public string DisplayName { get; set; }

        public IEnumerable<DeviceGroupCondition> Conditions { get; set; }
    }
}