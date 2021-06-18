// <copyright file="ColumnMappingController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Mmm.Iot.Config.Services.Models;
using Mmm.Iot.Config.WebService.Models;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Config.WebService.Controllers
{
    [Route("columnmapping")]
    [AllowAnonymous]
    public class ColumnMappingController : Controller
    {
        public List<ColumnMappingServiceModel> GetColumnMappings()
        {
            List<ColumnMappingServiceModel> columnMappings = new List<ColumnMappingServiceModel>();

            List<ColumnMappingDefinition> columnMappingDefinitions = new List<ColumnMappingDefinition>();
            List<ColumnMappingDefinition> customColumnMappingDefinitions = new List<ColumnMappingDefinition>();
            columnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "id", Mapping = "id" });
            columnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "lastActivity", Mapping = "lastActivity" });
            columnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "isSimulated", Mapping = "isSimulated" });
            columnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "c2DMessageCount", Mapping = "c2DMessageCount" });
            columnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "enabled", Mapping = "enabled" });
            columnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "lastStatusUpdated", Mapping = "lastStatusUpdated" });
            columnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "iotHubHostName", Mapping = "iotHubHostName" });
            columnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "eTag", Mapping = "eTag" });
            columnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "authentication", Mapping = "authentication" });
            columnMappingDefinitions.ForEach(x => x.IsDefault = true);

            customColumnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "methods", Mapping = "properties.reported.supportedMethods" });
            customColumnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "telemetry", Mapping = "properties.reported.telemetry" });
            customColumnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "type", Mapping = "properties.reported.type" });
            customColumnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "currentFwVersion", Mapping = "properties.reported.firmware.currentFwVersion" });
            customColumnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "previousFwVersion", Mapping = "previousProperties.reported.firmware.currentFwVersion" });
            customColumnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "lastFwUpdateStartTime", Mapping = "properties.reported.firmware.lastFwUpdateStartTime" });
            customColumnMappingDefinitions.Add(new ColumnMappingDefinition() { Name = "lastFwUpdateEndTime", Mapping = "properties.reported.firmware.lastFwUpdateEndTime" });

            columnMappings.Add(new ColumnMappingServiceModel() { Id = Guid.NewGuid().ToString(), Name = "Default", ColumnMappingDefinitions = columnMappingDefinitions, CreatedBy = "Ragav", CreatedDateTime = DateTime.UtcNow });
            columnMappings.Add(new ColumnMappingServiceModel() { Id = Guid.NewGuid().ToString(), Name = "Custom1", ColumnMappingDefinitions = customColumnMappingDefinitions, CreatedBy = "Ragav", CreatedDateTime = DateTime.UtcNow });
            columnMappings.Add(new ColumnMappingServiceModel() { Id = Guid.NewGuid().ToString(), Name = "Custom2", ColumnMappingDefinitions = customColumnMappingDefinitions, CreatedBy = "Ragav", CreatedDateTime = DateTime.UtcNow });
            columnMappings.Add(new ColumnMappingServiceModel() { Id = Guid.NewGuid().ToString(), Name = "Custom3", ColumnMappingDefinitions = customColumnMappingDefinitions, CreatedBy = "Ragav", CreatedDateTime = DateTime.UtcNow });

            return columnMappings;
        }

        [HttpPut("{id}")]
        public ColumnMappingApiModel SaveColumnMapping(string id, [FromBody] ColumnMappingApiModel columnMappingApiModel)
        {
            columnMappingApiModel.Id = id;
            return columnMappingApiModel;
        }
    }
}