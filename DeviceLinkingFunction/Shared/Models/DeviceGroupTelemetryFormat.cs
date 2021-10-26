// <copyright file="DeviceGroupTelemetryFormat.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.Functions.DeviceLinking.Shared.Models
{
    public class DeviceGroupTelemetryFormat
    {
        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("DisplayName")]
        public object DisplayName { get; set; }
    }
}