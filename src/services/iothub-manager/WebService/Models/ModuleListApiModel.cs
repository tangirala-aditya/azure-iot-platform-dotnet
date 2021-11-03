// <copyright file="ModuleListApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class ModuleListApiModel
    {
        public ModuleListApiModel()
        {
            this.Metadata = new Dictionary<string, string>
            {
                { "$type", $"DevicePropertyList;1" },
                { "$url", $"/v1/deviceproperties" },
            };
        }

        public ModuleListApiModel(ModuleServiceListModel modules)
        {
            this.Items = new List<ModuleApiModel>();
            modules.Items?.ForEach(module => this.Items.Add(new ModuleApiModel(module)));

            this.Metadata = new Dictionary<string, string>
            {
                { "$type", $"ModuleList;1" },
                { "$url", $"/v1/modules/deviceId" },
            };
        }

        [JsonProperty(PropertyName = "Items")]
        public List<ModuleApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata { get; set; }
    }
}