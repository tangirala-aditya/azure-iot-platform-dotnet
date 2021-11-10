// <copyright file="PingResultApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.IoTHubManager.Services.Models;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class PingResultApiModel
    {
        public PingResultApiModel(MethodResultServiceModel model)
        {
            this.Status = model.Status;
            this.Payload = model.JsonPayload;
        }

        public int Status { get; set; }

        public string Payload { get; set; }
    }
}