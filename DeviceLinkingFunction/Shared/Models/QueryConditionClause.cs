// <copyright file="QueryConditionClause.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.Functions.DeviceLinking.Shared.Models
{
    internal class QueryConditionClause
    {
        public string Key { get; set; }

        public string Operator { get; set; }

        public object Value { get; set; }
    }
}