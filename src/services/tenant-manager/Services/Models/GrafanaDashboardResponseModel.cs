// <copyright file="GrafanaDashboardResponseModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Mmm.Iot.TenantManager.Services.Models
{
    public class GrafanaDashboardResponseModel
    {
        public int Id { get; set; }

        public string Uid { get; set; }

        public string Url { get; set; }

        public string Status { get; set; }

        public string Version { get; set; }
    }
}