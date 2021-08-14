// <copyright file="TenantConnectionHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>
using System;
using Mmm.Iot.Common.Services.External.AppConfiguration;

namespace Mmm.Iot.Common.Services.Helpers
{
    public class TenantConnectionHelper
    {
        private const string TenantIotHubConnectionStringFormat = "tenant:{0}:iotHubConnectionString";
        private const string TenantEventHubConnectionStringFormat = "tenant:{0}:eventHubConn";
        private IAppConfigurationClient appConfigurationClient;

        public TenantConnectionHelper(IAppConfigurationClient appConfigurationClient)
        {
            this.appConfigurationClient = appConfigurationClient;
        }

        public string GetIotHubConnectionString(string tenantId)
        {
            var appConfigurationKey = string.Format(TenantIotHubConnectionStringFormat, tenantId);
            return this.appConfigurationClient.GetValue(appConfigurationKey);
        }

        public string GetEventHubConnectionString(string tenantId)
        {
            var appConfigurationKey = string.Format(TenantEventHubConnectionStringFormat, tenantId);
            try
            {
                var value = this.appConfigurationClient.GetValue(appConfigurationKey);
                return value;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}