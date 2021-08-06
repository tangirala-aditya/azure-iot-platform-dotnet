// <copyright file="GrafanaAPIKeyResponseModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Mmm.Iot.TenantManager.Services.Models
{
    public class GrafanaAPIKeyResponseModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }
    }
}