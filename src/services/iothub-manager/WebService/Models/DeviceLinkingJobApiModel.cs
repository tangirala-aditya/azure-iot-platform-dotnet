// <copyright file="DeviceLinkingJobApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.IoTHubManager.Services.Models;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class DeviceLinkingJobApiModel : AuditApiModel
    {
        public DeviceLinkingJobApiModel()
        {
        }

        public DeviceLinkingJobApiModel(DeviceLinkingJobServiceModel deviceLinkingJobServiceModel)
        {
            this.JobId = deviceLinkingJobServiceModel.JobId;
            this.Category = deviceLinkingJobServiceModel.Category.ToString();
            this.ParentDeviceId = deviceLinkingJobServiceModel.ParentDeviceId;
            this.DeviceGroupId = deviceLinkingJobServiceModel.DeviceGroupId;
            this.DeviceIds = deviceLinkingJobServiceModel.DeviceIds;
            this.JobStatus = deviceLinkingJobServiceModel.JobStatus;
            this.CreatedDate = deviceLinkingJobServiceModel.CreatedDate;
            this.CreatedBy = deviceLinkingJobServiceModel.CreatedBy;
            this.ModifiedBy = deviceLinkingJobServiceModel.ModifiedBy;
            this.ModifiedDate = deviceLinkingJobServiceModel.ModifiedDate;
        }

        public string JobId { get; set; }

        public string Category { get; set; }

        public string ParentDeviceId { get; set; }

        public string DeviceGroupId { get; set; }

        public IEnumerable<string> DeviceIds { get; set; }

        public string JobStatus { get; set; }
    }
}