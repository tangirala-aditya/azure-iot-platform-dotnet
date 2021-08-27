// <copyright file="GrafanaOrganizationRequestModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.TenantManager.Services.Models
{
    public class GrafanaOrganizationRequestModel
    {
        public GrafanaOrganizationRequestModel(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}