// <copyright file="RuleAction.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.DigitalTwin.Models
{
    public class RuleAction
    {
        public RuleActionType Type { get; }

        public Dictionary<string, object> Parameters { get; set; }
    }
}