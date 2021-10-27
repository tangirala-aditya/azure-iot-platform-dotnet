// <copyright file="DeviceLinkingJobApiListModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.IoTHubManager.Services.Models;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class DeviceLinkingJobApiListModel
    {
        public DeviceLinkingJobApiListModel()
        {
        }

        public DeviceLinkingJobApiListModel(DeviceLinkingJobServiceListModel deviceLinkingJobs)
        {
            this.Items = new List<DeviceLinkingJobApiModel>();
            foreach (var d in deviceLinkingJobs.Items)
            {
                this.Items.Add(new DeviceLinkingJobApiModel(d));
            }
        }

        public List<DeviceLinkingJobApiModel> Items { get; set; }
    }
}