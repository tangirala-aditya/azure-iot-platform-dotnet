// <copyright file="Program.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mmm.Iot.MigrateIoTDeviceTwinToADX.Config;
using Mmm.Iot.MigrateIoTDeviceTwinToADX.Helpers;

namespace Mmm.Iot.MigrateIoTDeviceTwinToADX
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var appConfig = InitializeAppConfig();
            ILogger logger = LoggerSetup();

            try
            {
                AppConfigHelper appConfigHelper = new AppConfigHelper(appConfig.AppConfigurationConnectionString);
                TableStorageHelper tableStorageHelper = new TableStorageHelper(appConfig);
                TenantConnectionHelper tenantConnectionHelper = new TenantConnectionHelper(appConfigHelper);
                DeviceGroupMigration deviceGroupMigration = new DeviceGroupMigration(tableStorageHelper, tenantConnectionHelper, logger, appConfig);
                logger.LogInformation("Device Group Migration Started");
                await deviceGroupMigration.Start();
                logger.LogInformation("Device Group Migration Completed");

                DeviceTwinMigration deviceTwinMigration = new DeviceTwinMigration(tableStorageHelper, tenantConnectionHelper, logger);
                logger.LogInformation("Device Twin Migration Started");
                await deviceTwinMigration.Start();
                logger.LogInformation("Device Twin Migration Completed");
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "Migration Failed");
            }
        }

        private static AppConfig InitializeAppConfig()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var assembly = typeof(Program).Assembly;
            configurationBuilder.AddJsonFile("appsettings.json", true);
            configurationBuilder.AddUserSecrets(assembly, true);

            configurationBuilder.AddEnvironmentVariables();
            var initialAppConfig = new AppConfig(configurationBuilder);
            configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddAzureAppConfiguration(initialAppConfig.AppConfigurationConnectionString);
            var azureAppConfigConfig = new AppConfig(configurationBuilder);

            var config = new ConfigurationBuilder();
            config.AddConfiguration(azureAppConfigConfig.Configuration);
            config.AddAzureKeyVault(
                $"https://{azureAppConfigConfig.Global.KeyVault.Name}.vault.azure.net/",
                azureAppConfigConfig.Global.AzureActiveDirectory.AppId,
                azureAppConfigConfig.Global.AzureActiveDirectory.AppSecret);
            config.AddConfiguration(initialAppConfig.Configuration);
            return new AppConfig(config);
        }

        private static ILogger LoggerSetup()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole();
            });

            ILogger logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Logger Setup completed");
            return logger;
        }
    }
}