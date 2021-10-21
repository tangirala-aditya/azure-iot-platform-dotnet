// <copyright file="ColumnMappingController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.Config.Services;
using Mmm.Iot.Config.WebService.Models;

namespace Mmm.Iot.Config.WebService.Controllers
{
    [Route("v1/columnmapping")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class ColumnMappingController : Controller
    {
        private readonly IStorage storage;

        public ColumnMappingController(IStorage storage)
        {
            this.storage = storage;
        }

        [HttpGet]
        [Authorize("ReadAll")]
        public async Task<ColumnMappingListApiModel> GetAllAsync()
        {
            return new ColumnMappingListApiModel(await this.storage.GetColumnMappingsAsync());
        }

        [HttpPost]
        [Authorize("ReadAll")]
        public async Task<ColumnMappingApiModel> CreateAsync([FromBody] ColumnMappingApiModel columnMappingApiModel)
        {
            return new ColumnMappingApiModel(await this.storage.AddColumnMappingAsync(columnMappingApiModel.ToServiceModel(), this.GetClaimsUserDetails()));
        }

        [HttpPut("{id}")]
        [Authorize("ReadAll")]
        public async Task<ColumnMappingApiModel> UpdateColumnMapping(string id, [FromBody] ColumnMappingApiModel columnMappingApiModel)
        {
            return new ColumnMappingApiModel(await this.storage.UpdateColumnMappingAsync(id, columnMappingApiModel.ToServiceModel(), this.GetClaimsUserDetails()));
        }

        [HttpDelete("{id}")]
        [Authorize("ReadAll")]
        public async Task DeleteColumnMapping(string id)
        {
            await this.storage.DeleteColumnMappingAsync(id, this.GetClaimsUserDetails());
        }

        [HttpGet("ColumnOptions")]
        [Authorize("ReadAll")]
        public async Task<ColumnOptionsListApiModel> GetDeviceGroupColumnOptions()
        {
            return new ColumnOptionsListApiModel(await this.storage.GetDeviceGroupColumnOptions());
        }

        [HttpPost("ColumnOptions")]
        [Authorize("UpdateDeviceGroups")]
        public async Task<ColumnOptionsApiModel> AddColumnOptionsAsync([FromBody] ColumnOptionsApiModel columnOptionsApiModel)
        {
            return new ColumnOptionsApiModel(await this.storage.AddColumnOptionsAsync(columnOptionsApiModel.ToServiceModel(), this.GetClaimsUserDetails()));
        }

        [HttpPut("ColumnOptions/{id}")]
        [Authorize("UpdateDeviceGroups")]
        public async Task<ColumnOptionsApiModel> UpdateColumnOptionsAsync(string id, [FromBody] ColumnOptionsApiModel columnOptionsApiModel)
        {
            return new ColumnOptionsApiModel(await this.storage.UpdateColumnOptionsAsync(id, columnOptionsApiModel.ToServiceModel(), this.GetClaimsUserDetails()));
        }
    }
}