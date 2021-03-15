// <copyright file="DeploymentRequest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Mmm.Iot.AsaManager.Services.Models.Storage
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