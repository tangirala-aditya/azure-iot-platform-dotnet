// <copyright file="ModuleServiceListModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class ModuleServiceListModel
    {
        public ModuleServiceListModel(IEnumerable<ModuleServiceModel> modules, string continuationToken = null)
        {
            this.ContinuationToken = continuationToken;
            this.Items = new List<ModuleServiceModel>(modules);
        }

        public string ContinuationToken { get; set; }

        public List<ModuleServiceModel> Items { get; set; }
    }
}