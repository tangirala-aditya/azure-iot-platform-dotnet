// <copyright file="GrafanaAPIKeyRequestModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.TenantManager.Services.Models
{
    public class GrafanaAPIKeyRequestModel
    {
        public GrafanaAPIKeyRequestModel(string appKeyName, GrafanaRoleType role, int secondsToLive = 0)
        {
            this.Name = appKeyName;
            this.Role = role;
            this.SecondsToLive = secondsToLive;
        }

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public GrafanaRoleType Role { get; set; }

        public int SecondsToLive { get; set; }
    }
}