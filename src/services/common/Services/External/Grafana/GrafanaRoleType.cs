// <copyright file="GrafanaRoleType.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.Common.Services.External.Grafana
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GrafanaRoleType
    {
        Viewer,
        Editor,
        Admin,
    }
}