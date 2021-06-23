// <copyright file="DeviceTwinModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.Messaging.Shared
{
    public class DeviceTwinModel
    {
        public string DeviceId { get; set; }

        public JObject Twin { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public DateTimeOffset DeviceCreatedDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}