// <copyright file="ModuleApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class ModuleApiModel
    {
        public ModuleApiModel()
        {
        }

        public ModuleApiModel(ModuleServiceModel module)
        {
            if (module == null)
            {
                return;
            }

            this.DeviceId = module.DeviceId;
            this.ModuleId = module.ModuleId;
            this.ETag = module.Etag;
            this.C2DMessageCount = module.C2DMessageCount;
            this.LastActivity = module.LastActivity;
            this.Enabled = module.Enabled;
            this.LastStatusUpdated = module.LastStatusUpdated;
            this.DeviceCreatedDate = module.DeviceCreatedDate;
            this.ModifiedDate = module.ModifiedDate;
            this.IoTHubHostName = module.IoTHubHostName;
            this.Authentication = new AuthenticationMechanismApiModel(
                module.Authentication ?? new AuthenticationMechanismServiceModel());

            if (module.Twin != null)
            {
                this.ETag = $"{this.ETag}|{module.Twin.ETag}";
                this.Properties = new TwinPropertiesApiModel(module.Twin.DesiredProperties, module.Twin.ReportedProperties);
                this.Tags = module.Twin.Tags;
            }
        }

        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "DeviceId")]
        public string DeviceId { get; set; }

        [JsonProperty(PropertyName = "ModuleId")]
        public string ModuleId { get; set; }

        [JsonProperty(PropertyName = "C2DMessageCount")]
        public int C2DMessageCount { get; set; }

        [JsonProperty(PropertyName = "LastActivity")]
        public DateTime LastActivity { get; set; }

        [JsonProperty(PropertyName = "Enabled")]
        public bool Enabled { get; set; }

        [JsonProperty(PropertyName = "LastStatusUpdated")]
        public DateTime LastStatusUpdated { get; set; }

        [JsonProperty(PropertyName = "DeviceCreatedDate")]
        public DateTime? DeviceCreatedDate { get; set; }

        [JsonProperty(PropertyName = "ModifiedDate")]
        public DateTime? ModifiedDate { get; set; }

        [JsonProperty(PropertyName = "IoTHubHostName")]
        public string IoTHubHostName { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Module;1" },
        };

        [JsonProperty(PropertyName = "Properties", NullValueHandling = NullValueHandling.Ignore)]
        public TwinPropertiesApiModel Properties { get; set; }

        [JsonProperty(PropertyName = "Tags", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Tags { get; set; }

        [JsonProperty(PropertyName = "Authentication")]
        public AuthenticationMechanismApiModel Authentication { get; set; }

        internal string ModuleEtag
        {
            get
            {
                if (!string.IsNullOrEmpty(this.ETag))
                {
                    var etags = this.ETag.Split('|');
                    if (etags.Length > 0)
                    {
                        return etags[0];
                    }
                }

                return "*";
            }
        }

        internal string TwinEtag
        {
            get
            {
                if (!string.IsNullOrEmpty(this.ETag))
                {
                    var etags = this.ETag.Split('|');
                    if (etags.Length > 1)
                    {
                        return etags[1];
                    }
                }

                return "*";
            }
        }
    }
}