// <copyright file="ColumnMappingDefinition.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Mmm.Iot.Config.Services.Models
{
    public class ColumnMappingDefinition
    {
        public string Name { get; set; }

        public string Mapping { get; set; }

        public string CellRenderer { get; set; }

        public string Description { get; set; }

        public bool IsDefault { get; set; }
    }
}