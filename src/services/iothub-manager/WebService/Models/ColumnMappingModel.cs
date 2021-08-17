// <copyright file="ColumnMappingModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class ColumnMappingModel
    {
        public ColumnMappingModel(string name, string mapping)
        {
            this.Name = name;
            this.Mapping = mapping;
        }

        public string Name { get; set; }

        public string Mapping { get; set; }
    }
}