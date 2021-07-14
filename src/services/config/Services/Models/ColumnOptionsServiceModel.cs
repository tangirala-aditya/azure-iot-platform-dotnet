// <copyright file="ColumnOptionsServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Config.Services.Models
{
    public class ColumnOptionsServiceModel : Audit
    {
        public string DeviceGroupId { get; set; }

        public string[] SelectedOptions { get; set; }

        public string ETag { get; set; }

        public string Key { get; set; }
    }
}