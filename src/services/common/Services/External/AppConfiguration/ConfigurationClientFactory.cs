// <copyright file="ConfigurationClientFactory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>
using System;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using Mmm.Iot.Common.Services.Config;

namespace Mmm.Iot.Common.Services.External.AppConfiguration
{
    public class ConfigurationClientFactory : IConfigurationClientFactory
    {
        private readonly AppConfig config;

        public ConfigurationClientFactory(AppConfig config)
        {
            this.config = config;
        }

        public ConfigurationClient Create()
        {
            if (!string.IsNullOrEmpty(this.config.AppConfigurationConnectionString))
            {
              return new ConfigurationClient(this.config.AppConfigurationConnectionString);
            }
            else
            {
              return new ConfigurationClient(new Uri(this.config.AppConfigEndpoint), new ManagedIdentityCredential(this.config.ManagedIdentityClientId));
            }
        }
    }
}