// <copyright file="ColumnOptionsApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Config.Services.Models;

namespace Mmm.Iot.Config.WebService.Models
{
    public class ColumnOptionsApiModel : AuditApiModel
    {
        public ColumnOptionsApiModel()
        {
        }

        public ColumnOptionsApiModel(ColumnOptionsServiceModel model)
        {
            this.DeviceGroupId = model.DeviceGroupId;
            this.SelectedOptions = model.SelectedOptions;
            this.ETag = model.ETag;
            this.Key = model.Key;
            this.CreatedBy = model.CreatedBy;
            this.CreatedDate = model.CreatedDate;
            this.ModifiedBy = model.ModifiedBy;
            this.ModifiedDate = model.ModifiedDate;
        }

        public string DeviceGroupId { get; set; }

        public string[] SelectedOptions { get; set; }

        public string ETag { get; set; }

        public string Key { get; set; }

        public ColumnOptionsServiceModel ToServiceModel()
        {
            return new ColumnOptionsServiceModel
            {
                SelectedOptions = this.SelectedOptions,
                DeviceGroupId = this.DeviceGroupId,
                ETag = this.ETag,
                Key = this.Key,
                CreatedBy = this.CreatedBy,
                ModifiedBy = this.ModifiedBy,
            };
        }
    }
}