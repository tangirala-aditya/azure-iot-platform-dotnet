// <copyright file="ModuleServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class ModuleServiceModel
    {
        public ModuleServiceModel(
            string etag,
            string deviceId,
            string moduleId,
            int c2DMessageCount,
            DateTime lastActivity,
            bool enabled,
            bool isEdgeDevice,
            DateTime lastStatusUpdated,
            TwinServiceModel twin,
            AuthenticationMechanismServiceModel authentication,
            DateTime? deviceCreatedDate = null,
            DateTime? modifiedDate = null)
        {
            this.Etag = etag;
            this.DeviceId = deviceId;
            this.ModuleId = moduleId;
            this.C2DMessageCount = c2DMessageCount;
            this.LastActivity = lastActivity;
            this.Enabled = enabled;
            this.LastStatusUpdated = lastStatusUpdated;
            this.Twin = twin;
            this.Authentication = authentication;
            this.DeviceCreatedDate = deviceCreatedDate;
            this.ModifiedDate = modifiedDate;
        }

        public ModuleServiceModel(Twin azureTwin)
            : this(
                etag: azureTwin.ETag,
                deviceId: azureTwin.DeviceId,
                moduleId: azureTwin.ModuleId,
                c2DMessageCount: azureTwin.CloudToDeviceMessageCount ?? azureTwin.CloudToDeviceMessageCount ?? 0,
                lastActivity: azureTwin.LastActivityTime ?? azureTwin.LastActivityTime ?? default,
                enabled: azureTwin.Status.Equals(DeviceStatus.Enabled),
                isEdgeDevice: azureTwin.Capabilities?.IotEdge ?? azureTwin.Capabilities?.IotEdge ?? false,
                lastStatusUpdated: azureTwin.StatusUpdatedTime ?? azureTwin.StatusUpdatedTime ?? default,
                twin: new TwinServiceModel(azureTwin),
                authentication: null)
        {
        }

        public string Etag { get; set; }

        public string DeviceId { get; set; }

        public string ModuleId { get; set; }

        public int C2DMessageCount { get; set; }

        public DateTime LastActivity { get; set; }

        public bool Enabled { get; set; }

        public DateTime LastStatusUpdated { get; set; }

        public DateTime? DeviceCreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public TwinServiceModel Twin { get; set; }

        public string IoTHubHostName { get; set; }

        public string Status { get; set; }

        public AuthenticationMechanismServiceModel Authentication { get; set; }
    }
}