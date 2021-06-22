// <copyright file="ColumnMappingListApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Mmm.Iot.Config.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.Config.WebService.Models
{
    public class ColumnMappingListApiModel
    {
        public ColumnMappingListApiModel(IEnumerable<ColumnMappingServiceModel> columnMappings)
        {
            this.Items = new List<ColumnMappingApiModel>();

            foreach (var columnMapping in columnMappings)
            {
                this.Items.Add(new ColumnMappingApiModel(columnMapping));
            }

            this.Metadata = new Dictionary<string, string>
            {
                { "$type", $"ColumnMappingsList;1" },
                { "$url", $"/v1/columnmapings" },
            };
        }

        public ColumnMappingListApiModel()
        {
        }

        [JsonProperty("Items")]
        public List<ColumnMappingApiModel> Items { get; set; }

        [JsonProperty("$metadata")]
        public Dictionary<string, string> Metadata { get; set; }
    }
}