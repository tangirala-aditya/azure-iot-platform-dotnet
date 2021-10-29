// <copyright file="GrafanaOrganizationRequestModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.Common.Services.External.Grafana
{
    public class GrafanaOrganizationRequestModel
    {
        public GrafanaOrganizationRequestModel(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}