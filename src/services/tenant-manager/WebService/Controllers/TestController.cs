// <copyright file="TestController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.External.KustoStorage;
using Mmm.Iot.Common.Services.Filters;

namespace Mmm.Iot.TenantManager.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class TestController : Controller
    {
        private IKustoCluterManagementClient kustoCluterManagementClient;
        private IKustoTableManagementClient kustoTableManagementClient;

        public TestController(IKustoCluterManagementClient kustoCluterManagementClient, IKustoTableManagementClient kustoTableManagementClient)
        {
            this.kustoCluterManagementClient = kustoCluterManagementClient;
            this.kustoTableManagementClient = kustoTableManagementClient;
        }

        [HttpGet("")]
        public async Task GetAsync()
        {
            string tenantId = this.GetTenantId();

            Console.WriteLine("Creating a DB in Data Explorer");

            var softDeletePeriod = new TimeSpan(60, 0, 0, 0);
            var databaseName = $"IoT-{tenantId}";

            await this.kustoCluterManagementClient.CreatedDBInCluterAsync(databaseName, softDeletePeriod);

            Console.WriteLine($"Created a {tenantId} DB in Data Explorer");

            Console.WriteLine($"Creating telemetry table and mapping in {tenantId} DB in Data Explorer");

            var tableName = "Telemetry";
            var tableMappingName = $"TelemetryEvents_JSON_Mapping-{tenantId}";
            var tableSchema = new[]
            {
                                    Tuple.Create("DeviceId", "System.String"),
                                    Tuple.Create("Data", "System.Object"),
                                    Tuple.Create("TimeStamp", "System.Datetime"),
            };
            var mappingSchema = new ColumnMapping[]
            {
                                    new ColumnMapping() { ColumnName = "DeviceId", ColumnType = "string", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.iothub-connection-device-id" } } },
                                    new ColumnMapping() { ColumnName = "Data", ColumnType = "dynamic", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$" } } },
                                    new ColumnMapping() { ColumnName = "TimeStamp", ColumnType = "datetime", Properties = new Dictionary<string, string>() { { MappingConsts.Path, "$.iothub-enqueuedtime" } } },
            };

            this.kustoTableManagementClient.CreateTable(tableName, tableSchema, databaseName);

            this.kustoTableManagementClient.CreateTableMapping(tableMappingName, mappingSchema, tableName, databaseName);

            this.kustoTableManagementClient.EnableStreamingIngestionPolicyToTable(tableName, databaseName);

            string dataConnectName = $"TelemetryDataConnect-{tenantId.Substring(0, 8)}";
            string iotHubName = $"iothub-{tenantId.Substring(0, 8)}";
            string iotHubConsumerGroup = "$Default";

            await this.kustoCluterManagementClient.AddIoTHubDataConnectionAsync(dataConnectName, databaseName, tableName, tableMappingName, iotHubName, iotHubConsumerGroup);
        }
    }
}