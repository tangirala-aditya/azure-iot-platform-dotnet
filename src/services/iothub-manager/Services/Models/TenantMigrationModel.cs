// <copyright file="TenantMigrationModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Cosmos.Table;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class TenantMigrationModel : TableEntity
    {
        public TenantMigrationModel()
        {
        }

        public TenantMigrationModel(string id, bool migrationStatus)
        {
            // Use the first character of the tenant id as the partion key as it is randomly distributed
            this.PartitionKey = id.Substring(0, 1);
            this.RowKey = id;
            this.TenantId = id;
            this.IsMigrationCompleted = migrationStatus;
        }

        public string TenantId { get; set; }

        public bool IsMigrationCompleted { get; set; }
    }
}