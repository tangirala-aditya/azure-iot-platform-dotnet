// <copyright file="GlobalConfig.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.MigrateIoTDeviceTwinToADX.Config
{
    public partial class GlobalConfig
    {
        public virtual AzureActiveDirectoryConfig AzureActiveDirectory { get; set; }

        public virtual KeyVaultConfig KeyVault { get; set; }

        public virtual string StorageAccountConnectionStringKeyVaultSecret { get; set; }

        public virtual string StorageAccountConnectionString { get; set; }

        public virtual CosmosDb CosmosDb { get; set; }
    }
}