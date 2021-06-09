// <copyright file="TelemetryModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Common.Services.External.KustoStroage
{
    public class TelemetryModel
    {
        public string DeviceId { get; set; }

        public JObject Data { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}