// <copyright file="Payload.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class Payload
    {
        public int Loglevel { get; set; }

        public string Text { get; set; }

        public string Timestamp { get; set; }
    }
}