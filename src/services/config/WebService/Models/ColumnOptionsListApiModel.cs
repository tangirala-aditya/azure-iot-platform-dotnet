// <copyright file="ColumnOptionsListApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Mmm.Iot.Config.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.Config.WebService.Models
{
    public class ColumnOptionsListApiModel
    {
        public ColumnOptionsListApiModel(IEnumerable<ColumnOptionsServiceModel> columnOptions)
        {
            this.Items = new List<ColumnOptionsApiModel>();

            foreach (var columnOption in columnOptions)
            {
                this.Items.Add(new ColumnOptionsApiModel(columnOption));
            }

            this.Metadata = new Dictionary<string, string>
            {
                { "$type", $"ColumnOptionsList;1" },
                { "$url", $"/v1/columnmapings/ColumnOptions" },
            };
        }

        public ColumnOptionsListApiModel()
        {
        }

        [JsonProperty("Items")]
        public List<ColumnOptionsApiModel> Items { get; set; }

        [JsonProperty("$metadata")]
        public Dictionary<string, string> Metadata { get; set; }
    }
}