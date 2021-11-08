// <copyright file="PayloadApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class PayloadApiModel
    {
        public string Id { get; set; }

        public string PayloadBytes { get; set; }

        public string Payload { get; set; }
    }
}