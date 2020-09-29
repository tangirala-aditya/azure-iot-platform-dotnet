// <copyright file="UserSettingsModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Cosmos.Table;

namespace TenantDataManagement
{
    public class UserSettingsModel : TableEntity
    {
        public UserSettingsModel()
        {
        }

        public UserSettingsModel(DynamicTableEntity tableEntity)
        {
            this.PartitionKey = tableEntity.PartitionKey;
            this.RowKey = tableEntity.RowKey;
            this.Value = tableEntity.Properties["Value"].StringValue;
        }

        public string Value { get; set; }

        // Define aliases for the partition and row keys
        public string UserId
        {
            get
            {
                return this.PartitionKey;
            }
        }

        public string SettingKey
        {
            get
            {
                return this.RowKey;
            }
        }

        public static explicit operator UserSettingsModel(DynamicTableEntity v) => new UserSettingsModel(v);
    }
}