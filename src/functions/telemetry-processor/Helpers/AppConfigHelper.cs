// <copyright file="AppConfigHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Mmm.Iot.Functions.TelemetryProcessor.Helpers
{
    public class AppConfigHelper
    {
        private readonly IConfiguration configuration;
        private readonly IConfigurationRefresher configurationRefresher;
        private ConfigurationClient client;

        public AppConfigHelper(string appConfigurationConnectionString)
        {
            this.client = new ConfigurationClient(appConfigurationConnectionString);
        }

        public AppConfigHelper(IConfiguration configuration, IConfigurationRefresher configurationRefresher)
        {
            this.configuration = configuration;
            this.configurationRefresher = configurationRefresher;
        }

        public async Task<string> GetValueByKey(string key)
        {
            var configurationSetting = this.configuration[key];
            if (configurationSetting == null)
            {
                await this.configurationRefresher.RefreshAsync();
                configurationSetting = this.configuration[key];
            }

            return configurationSetting;
        }
    }
}