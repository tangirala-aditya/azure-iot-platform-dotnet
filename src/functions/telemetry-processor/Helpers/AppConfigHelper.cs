// <copyright file="AppConfigHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.AppConfiguration;

namespace Mmm.Iot.Functions.TelemetryProcessor.Helpers
{
    public class AppConfigHelper
    {
        private ConfigurationClient client;

        public AppConfigHelper(string appConfigurationConnectionString)
        {
            this.client = new ConfigurationClient(appConfigurationConnectionString);
        }

        public async Task<string> GetValueByKey(string key)
        {
            ConfigurationSetting configurationSetting = await this.client.GetConfigurationSettingAsync(key);
            if (configurationSetting != null)
            {
                return configurationSetting.Value;
            }

            return string.Empty;
        }
    }
}