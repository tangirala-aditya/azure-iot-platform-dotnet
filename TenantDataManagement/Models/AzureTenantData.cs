// <copyright file="AzureTenantData.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;

namespace TenantDataManagement
{
    public class AzureTenantData
    {
        public string Region { get; set; }

        public long DeviceCount { get; set; }

        public DateTimeOffset? LastAccessed { get; set; }

        public string TenantId { get; set; }

        public string TenantName { get; set; }

        public string Subscription { get; set; }

        public string ResourceGroup { get; set; }

        public int UserCount { get; set; }

        public string IoTHubName { get; set; }

        public bool IoTHubIsDeployed { get; set; }

        public DateTimeOffset LastDeviceUpdate { get; set; }

        public string LastDeviceDataReceived { get; set; }

        public string SAJob { get; set; }

        public int DeploymentCount { get; set; }

        public int PackageCount { get; set; }
    }
}