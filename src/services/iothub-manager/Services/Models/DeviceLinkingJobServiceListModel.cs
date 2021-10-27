// <copyright file="DeviceLinkingJobServiceListModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceLinkingJobServiceListModel
    {
        public DeviceLinkingJobServiceListModel()
        {
        }

        public DeviceLinkingJobServiceListModel(IEnumerable<DeviceLinkingJobServiceModel> deviceLinkingJobs)
        {
            this.Items = deviceLinkingJobs;
        }

        public IEnumerable<DeviceLinkingJobServiceModel> Items { get; set; }
    }
}