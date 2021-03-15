// <copyright file="DeploymentRequest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.DeploymentSync.Shared.Models
{
    public class DeploymentRequest
    {
        public DeploymentRequest()
        {
            this.Devices = new List<string>();
        }

        public string TenantId { get; set; }

        public string DeviceGroupId { get; set; }

        public List<string> Devices { get; set; }
    }
}