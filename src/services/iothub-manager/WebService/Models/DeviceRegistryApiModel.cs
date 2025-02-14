// <copyright file="DeviceRegistryApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class DeviceRegistryApiModel
    {
        public DeviceRegistryApiModel()
        {
        }

        public DeviceRegistryApiModel(DeviceServiceModel device)
        {
            if (device == null)
            {
                return;
            }

            this.Id = device.Id;
            this.ETag = device.Etag;
            this.C2DMessageCount = device.C2DMessageCount;
            this.LastActivity = device.LastActivity;
            this.Connected = device.Connected;
            this.Enabled = device.Enabled;
            this.IsEdgeDevice = device.IsEdgeDevice;
            this.LastStatusUpdated = device.LastStatusUpdated;
            this.DeviceCreatedDate = device.DeviceCreatedDate;
            this.ModifiedDate = device.ModifiedDate;
            this.IoTHubHostName = device.IoTHubHostName;
            this.Authentication = new AuthenticationMechanismApiModel(
                device.Authentication ?? new AuthenticationMechanismServiceModel());

            if (device.Twin != null)
            {
                this.ETag = $"{this.ETag}|{device.Twin.ETag}";
                this.Properties = new TwinPropertiesApiModel(device.Twin.DesiredProperties, device.Twin.ReportedProperties);
                this.Tags = device.Twin.Tags;
                this.IsSimulated = device.Twin.IsSimulated;
            }

            if (device.PreviousTwin != null)
            {
                this.PreviousProperties = new TwinPropertiesApiModel(device.PreviousTwin.DesiredProperties, device.PreviousTwin.ReportedProperties);
            }
        }

        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "C2DMessageCount")]
        public int C2DMessageCount { get; set; }

        [JsonProperty(PropertyName = "LastActivity")]
        public DateTime LastActivity { get; set; }

        [JsonProperty(PropertyName = "Connected")]
        public bool Connected { get; set; }

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
            { "$type", "Device;1" },
            { "$uri", "/" + "v1/devices/" + this.Id },
            { "$twin_uri", "/" + "v1/devices/" + this.Id + "/twin" },
        };

        [JsonProperty(PropertyName = "Properties", NullValueHandling = NullValueHandling.Ignore)]
        public TwinPropertiesApiModel Properties { get; set; }

        [JsonProperty(PropertyName = "PreviousProperties", NullValueHandling = NullValueHandling.Ignore)]
        public TwinPropertiesApiModel PreviousProperties { get; set; }

        [JsonProperty(PropertyName = "Tags", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Tags { get; set; }

        [JsonProperty(PropertyName = "IsEdgeDevice")]
        public bool IsEdgeDevice { get; set; }

        [JsonProperty(PropertyName = "IsSimulated", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsSimulated { get; set; }

        [JsonProperty(PropertyName = "Authentication")]
        public AuthenticationMechanismApiModel Authentication { get; set; }

        internal string DeviceRegistryEtag
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

        public DeviceServiceModel ToServiceModel()
        {
            var twinModel = new TwinServiceModel(
                etag: this.TwinEtag,
                deviceId: this.Id,
                desiredProperties: this.Properties?.Desired,
                reportedProperties: this.Properties?.Reported,
                tags: this.Tags,
                isSimulated: this.IsSimulated);

            return new DeviceServiceModel(
                etag: this.DeviceRegistryEtag,
                id: this.Id,
                c2DMessageCount: this.C2DMessageCount,
                lastActivity: this.LastActivity,
                connected: this.Connected,
                enabled: this.Enabled,
                isEdgeDevice: this.IsEdgeDevice,
                lastStatusUpdated: this.LastStatusUpdated,
                deviceCreatedDate: this.DeviceCreatedDate,
                modifiedDate: this.ModifiedDate,
                twin: twinModel,
                ioTHubHostName: this.IoTHubHostName,
                authentication: this.Authentication == null ? null : this.Authentication.ToServiceModel());
        }
    }
}