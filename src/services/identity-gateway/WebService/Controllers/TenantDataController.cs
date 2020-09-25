// <copyright file="TenantDataController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.IdentityGateway.Services;

namespace Mmm.Iot.IdentityGateway.WebService.Controllers
{
    [Route("v1/[controller]")]
    public class TenantDataController : Controller
    {
        private readonly TenantDataCollectorService tenantDataCollectorService;

        public TenantDataController(TenantDataCollectorService tenantDataCollectorService)
        {
            this.tenantDataCollectorService = tenantDataCollectorService;
        }

        [HttpGet("GetResourceGroups")]
        public async Task<JsonResult> GetResourceGroups()
        {
            // return "Text";
            var x = await this.tenantDataCollectorService.GetResourceGroups();

            return this.Json(x);
        }

        [HttpGet("GetResourceByResourceGroup")]
        public async Task<JsonResult> GetResourceByResourceGroup()
        {
            // return "Text";
            var x = await this.tenantDataCollectorService.GetResourcesByResourceGroups();

            return this.Json(x);
        }
    }
}