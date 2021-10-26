// <copyright file="DeviceGroupCondition.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.Functions.DeviceLinking.Shared.Models
{
    public class DeviceGroupCondition
    {
        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("Operator")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceGroupConditionOperatorType Operator { get; set; }

        [JsonProperty("Value")]
        public object Value { get; set; }
    }
}