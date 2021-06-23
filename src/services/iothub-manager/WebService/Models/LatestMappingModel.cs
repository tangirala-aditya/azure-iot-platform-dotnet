// <copyright file="LatestMappingModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class LatestMappingModel
    {
        public LatestMappingModel(string name, string mapping)
        {
            this.Name = name;
            this.Mapping = mapping;
        }

        public string Name { get; set; }

        public string Mapping { get; set; }
    }
}