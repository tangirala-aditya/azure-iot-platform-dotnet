// <copyright file="TenantConnectionHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Devices;

namespace Mmm.Iot.MigrateIoTDeviceTwinToADX.Helpers
{
    public class TenantConnectionHelper
    {
        private const string TenantIotHubConnectionStringFormat = "tenant:{0}:iotHubConnectionString";
        private const string TenantEventHubConnectionStringFormat = "tenant:{0}:eventHubConn";
        private AppConfigHelper appConfigHelper;

        public TenantConnectionHelper(AppConfigHelper config)
        {
            this.appConfigHelper = config;
        }

        public string GetIotHubConnectionString(string tenantId)
        {
            var appConfigurationKey = string.Format(TenantIotHubConnectionStringFormat, tenantId);
            return this.appConfigHelper.GetValue(appConfigurationKey);
        }

        public string GetEventHubConnectionString(string tenantId)
        {
            var appConfigurationKey = string.Format(TenantEventHubConnectionStringFormat, tenantId);
            var value = this.appConfigHelper.GetValue(appConfigurationKey);
            return value;
        }

        public RegistryManager GetRegistry(string tenantId)
        {
            RegistryManager registry = null;

            IoTHubConnectionHelper.CreateUsingHubConnectionString(this.GetIotHubConnectionString(tenantId), (conn) =>
            {
                registry = RegistryManager.CreateFromConnectionString(conn);
            });
            if (registry == null)
            {
                throw new Exception($"Invalid tenant information for HubConnectionstring.");
            }

            return registry;
        }
    }
}