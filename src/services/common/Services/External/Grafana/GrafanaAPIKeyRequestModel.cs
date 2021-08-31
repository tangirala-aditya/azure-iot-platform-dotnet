// <copyright file="GrafanaAPIKeyRequestModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.Common.Services.External.Grafana
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