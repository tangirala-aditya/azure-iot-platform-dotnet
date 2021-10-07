// <copyright file="AzureActiveDirectoryConfig.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.MigrateIoTDeviceTwinToADX.Config
{
    public partial class AzureActiveDirectoryConfig
    {
        public virtual string AppId { get; set; }

        public virtual string AppSecret { get; set; }

        public virtual string TenantId { get; set; }
    }
}