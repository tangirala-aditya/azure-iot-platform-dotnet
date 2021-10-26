// <copyright file="TenantConnectionHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Devices;
using Mmm.Iot.Functions.DeviceLinking.Shared.Exceptions;

namespace Mmm.Iot.Functions.DeviceLinking.Shared.Helpers
{
    public static class TenantConnectionHelper
    {
        private const string TenantIotHubConnectionStringFormat = "tenant:{0}:iotHubConnectionString";
        private const string TenantEventHubConnectionStringFormat = "tenant:{0}:eventHubConn";

        public static string GetIotHubConnectionString(string tenantId)
        {
            var appConfigurationKey = string.Format(TenantIotHubConnectionStringFormat, tenantId);

            string connectionString = Environment.GetEnvironmentVariable("AppConfigurationConnectionString", EnvironmentVariableTarget.Process);

            AppConfigHelper appConfigHelper = new AppConfigHelper(connectionString);
            return appConfigHelper.GetValue(appConfigurationKey);
        }

        public static string GetEventHubConnectionString(string tenantId)
        {
            var appConfigurationKey = string.Format(TenantEventHubConnectionStringFormat, tenantId);

            string connectionString = Environment.GetEnvironmentVariable("AppConfigurationConnectionString", EnvironmentVariableTarget.Process);

            AppConfigHelper appConfigHelper = new AppConfigHelper(connectionString);
            var value = appConfigHelper.GetValue(appConfigurationKey);
            return value;
        }

        public static RegistryManager GetRegistry(string tenantId)
        {
            RegistryManager registry = null;

            IoTHubConnectionHelper.CreateUsingHubConnectionString(GetIotHubConnectionString(tenantId), (conn) =>
               {
                   registry = RegistryManager.CreateFromConnectionString(conn);
               });
            if (registry == null)
            {
                throw new InvalidConfigurationException($"Invalid tenant information for HubConnectionstring.");
            }

            return registry;
        }
    }
}