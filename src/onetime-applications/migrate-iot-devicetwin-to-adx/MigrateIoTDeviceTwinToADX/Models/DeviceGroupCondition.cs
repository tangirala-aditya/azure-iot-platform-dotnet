// <copyright file="DeviceGroupCondition.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MigrateIoTDeviceTwinToADX.Models
{
    public class DeviceGroupCondition
    {
        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceGroupConditionOperatorType Operator { get; set; }

        [JsonProperty("Value")]
        public object Value { get; set; }
    }
}