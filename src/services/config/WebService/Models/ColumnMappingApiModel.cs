// <copyright file="ColumnMappingApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Config.Services.Models;

namespace Mmm.Iot.Config.WebService.Models
{
    public class ColumnMappingApiModel : AuditApiModel
    {
        public ColumnMappingApiModel()
        {
        }

        public ColumnMappingApiModel(ColumnMappingServiceModel columnMappingServiceModel)
        {
            this.Name = columnMappingServiceModel.Name;
            this.ColumnMappingDefinitions = columnMappingServiceModel.ColumnMappingDefinitions;
            this.Id = columnMappingServiceModel.Id;
            this.ETag = columnMappingServiceModel.ETag;
            this.CreatedBy = columnMappingServiceModel.CreatedBy;
            this.CreatedDate = columnMappingServiceModel.CreatedDate;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<ColumnMappingDefinition> ColumnMappingDefinitions { get; set; }

        public string ETag { get; set; }

        public bool IsDefault { get; set; }

        public ColumnMappingServiceModel ToServiceModel()
        {
            return new ColumnMappingServiceModel
            {
                Name = this.Name,
                ColumnMappingDefinitions = this.ColumnMappingDefinitions,
                ETag = this.ETag,
                Id = this.Id,
                IsDefault = this.IsDefault,
            };
        }
    }
}