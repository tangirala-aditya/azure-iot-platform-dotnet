// <copyright file="Startup.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(Mmm.Iot.Functions.TelemetryProcessor.Startup))]

namespace Mmm.Iot.Functions.TelemetryProcessor
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddAzureAppConfiguration();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
            {
                options.Connect(Environment.GetEnvironmentVariable("AppConfigConnectionString"))

                       // Load all keys that start with `tenant:`
                       .Select("tenant:*")

                       // Configure to reload configuration if the registered 'tenant:refreshappconfig' key is modified
                       .ConfigureRefresh(refreshOptions =>
                          refreshOptions.Register("tenant:refreshappconfig", refreshAll: true));
            });
        }
    }
}