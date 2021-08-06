// <copyright file="GrafanaRoleType.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.TenantManager.Services.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GrafanaRoleType
    {
        Viewer,
        Editer,
        Admin,
    }
}