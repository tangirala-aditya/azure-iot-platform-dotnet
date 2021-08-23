// <copyright file="DevicesController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.IoTHubManager.WebService.Models;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.IoTHubManager.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class DevicesController : Controller
    {
        private const string ContinuationTokenName = "x-ms-continuation";
        private readonly IDevices devices;
        private readonly IDeviceProperties deviceProperties;
        private readonly IDeviceService deviceService;

        public DevicesController(IDevices devices, IDeviceService deviceService, IDeviceProperties deviceProperties)
        {
            this.deviceProperties = deviceProperties;
            this.devices = devices;
            this.deviceService = deviceService;
        }

        [HttpGet]
        [Authorize("ReadAll")]
        public async Task<DeviceListApiModel> GetDevicesAsync([FromQuery] string query)
        {
            string continuationToken = string.Empty;
            if (this.Request.Headers.ContainsKey(ContinuationTokenName))
            {
                continuationToken = this.Request.Headers[ContinuationTokenName].FirstOrDefault();
            }

            return new DeviceListApiModel(await this.devices.GetListAsync(query, continuationToken));
        }

        [HttpGet("statistics")]
        [Authorize("ReadAll")]
        public async Task<DeviceStatisticsApiModel> GetDevicesStatisticsAsync([FromQuery] string query)
        {
            return new DeviceStatisticsApiModel(await this.devices.GetDeviceStatisticsAsync(query));
        }

        [HttpPost("query")]
        [Authorize("ReadAll")]
        public async Task<DeviceListApiModel> QueryDevicesAsync([FromBody] string query)
        {
            string continuationToken = string.Empty;
            if (this.Request.Headers.ContainsKey(ContinuationTokenName))
            {
                continuationToken = this.Request.Headers[ContinuationTokenName].FirstOrDefault();
            }

            return new DeviceListApiModel(await this.devices.GetListAsync(query, continuationToken));
        }

        [HttpGet("deploymentHistory/{id}")]
        [Authorize("ReadAll")]
        public async Task<DeploymentHistoryListModel> GetDeviceDeploymentAsync(string id)
        {
            return await this.devices.GetDeploymentHistoryAsync(id, this.GetTenantId());
        }

        [HttpGet("{id}")]
        [Authorize("ReadAll")]
        public async Task<DeviceRegistryApiModel> GetDeviceAsync(string id)
        {
            return new DeviceRegistryApiModel(await this.devices.GetAsync(id));
        }

        [HttpPost]
        [Authorize("CreateDevices")]
        public async Task<DeviceRegistryApiModel> PostAsync([FromBody] DeviceRegistryApiModel device)
        {
            return new DeviceRegistryApiModel(await this.devices.CreateAsync(device.ToServiceModel()));
        }

        [HttpPut("{id}")]
        [Authorize("UpdateDevices")]
        public async Task<DeviceRegistryApiModel> PutAsync(string id, [FromBody] DeviceRegistryApiModel device)
        {
            DevicePropertyDelegate updateListDelegate = new DevicePropertyDelegate(this.deviceProperties.UpdateListAsync);
            return new DeviceRegistryApiModel(await this.devices.UpdateAsync(device.ToServiceModel(), updateListDelegate));
        }

        [HttpDelete("{id}")]
        [Authorize("DeleteDevices")]
        public async Task DeleteAsync(string id)
        {
            await this.devices.DeleteAsync(id);
        }

        [HttpPost("{id}/methods")]
        [Authorize("CreateJobs")]
        public async Task<MethodResultApiModel> InvokeDeviceMethodAsync(string id, [FromBody] MethodParameterApiModel parameter)
        {
            return new MethodResultApiModel(await this.deviceService.InvokeDeviceMethodAsync(id, parameter.ToServiceModel()));
        }

        [HttpPost("{id}/c2dmessage")]
        [Authorize("SendC2DMessages")]
        public async Task SendCloudToDeviceAsync(string id, [FromBody] string message)
        {
            await this.deviceService.SendCloudToDeviceMessage(id, message);
        }

        [HttpGet("files/{id}")]
        [Authorize("ReadAll")]
        public async Task<List<string>> GetDeviceFilesAsync(string id)
        {
            return await this.deviceProperties.GetUploadedFilesForDevice(this.GetTenantId(), id);
        }

        [HttpPost("report")]
        [Authorize("ReadAll")]
        public async Task<IActionResult> ExportDevicesReport([FromQuery] string query, [FromBody] List<ColumnMappingModel> columnMapping)
        {
            var deviceList = new DeviceListApiModel(await this.devices.GetListAsync(query, null));

            var stream = new MemoryStream();

            using (SpreadsheetDocument package = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                this.CreatePartsForExcel(package, deviceList.Items, columnMapping);
            }

            stream.Position = 0;
            string excelName = $"DeploymentReport-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";

            return this.File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
        }

        private void CreatePartsForExcel(SpreadsheetDocument document, List<DeviceRegistryApiModel> data, List<ColumnMappingModel> columnMapping)
        {
            SheetData partSheetData = this.GenerateSheetdataForDetails(data, columnMapping);

            WorkbookPart workbookPart = document.AddWorkbookPart();
            this.GenerateWorkbookPartContent(workbookPart);

            WorkbookStylesPart workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>("rId3");
            OpenXMLHelper.GenerateWorkbookStylesPartContent(workbookStylesPart);

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>("rId1");
            OpenXMLHelper.GenerateWorksheetPartContent(worksheetPart, partSheetData);
        }

        private void GenerateWorkbookPartContent(WorkbookPart workbookPart)
        {
            Workbook workbook = new Workbook();
            Sheets sheets = new Sheets();
            Sheet sheet = new Sheet() { Name = "Device Report", SheetId = (UInt32Value)1U, Id = "rId1" };
            sheets.Append(sheet);
            workbook.Append(sheets);
            workbookPart.Workbook = workbook;
        }

        private SheetData GenerateSheetdataForDetails(List<DeviceRegistryApiModel> data, List<ColumnMappingModel> columnMapping)
        {
            SheetData sheetData = new SheetData();
            sheetData.Append(this.CreateHeaderRowForExcel(columnMapping));

            foreach (DeviceRegistryApiModel model in data)
            {
                Row partsRows = this.GenerateRowForChildPartDetail(model, columnMapping);
                sheetData.Append(partsRows);
            }

            return sheetData;
        }

        private Row CreateHeaderRowForExcel(List<ColumnMappingModel> columnMapping)
        {
            Row workRow = new Row();
            foreach (var t in columnMapping)
            {
                workRow.Append(OpenXMLHelper.CreateCell(t.Name, 2U));
            }

            return workRow;
        }

        private Row GenerateRowForChildPartDetail(DeviceRegistryApiModel model, List<ColumnMappingModel> columnMapping)
        {
            Row tRow = new Row();
            JObject modelObject = (JObject)JToken.FromObject(model);
            foreach (var map in columnMapping)
            {
                tRow.Append(OpenXMLHelper.CreateCell((string)modelObject.SelectToken(map.Mapping)));
            }

            return tRow;
        }
    }
}