// <copyright file="ColumnMappingServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Config.Services.Models
{
    public class ColumnMappingServiceModel : Audit
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<ColumnMappingDefinition> ColumnMappingDefinitions { get; set; }

        public string ETag { get; set; }

        public bool IsDefault { get; set; }

        public IList<string> Tags { get; set; }
    }
}