// <copyright file="DeviceGroupColumnOptionsApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Config.Services.Models;

namespace Mmm.Iot.Config.WebService.Models
{
    public class DeviceGroupColumnOptionsApiModel : AuditApiModel
    {
        public DeviceGroupColumnOptionsApiModel()
        {
        }

        public DeviceGroupColumnOptionsApiModel(DeviceGroupColumnOptions model)
        {
            this.DeviceGroupId = model.DeviceGroupId;
            this.SelectedOptions = model.SelectedOptions;
        }

        public string DeviceGroupId { get; set; }

        public string[] SelectedOptions { get; set; }

        public DeviceGroupColumnOptions ToServiceModel()
        {
            return new DeviceGroupColumnOptions
            {
                SelectedOptions = this.SelectedOptions,
                DeviceGroupId = this.DeviceGroupId,
            };
        }
    }
}