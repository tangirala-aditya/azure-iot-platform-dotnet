// <copyright file="DeviceGroupColumnOptions.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Config.Services.Models
{
    public class DeviceGroupColumnOptions : Audit
    {
        public string DeviceGroupId { get; set; }

        public string[] SelectedOptions { get; set; }
    }
}