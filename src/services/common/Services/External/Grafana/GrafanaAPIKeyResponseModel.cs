// <copyright file="GrafanaAPIKeyResponseModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.Common.Services.External.Grafana
{
    public class GrafanaAPIKeyResponseModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }
    }
}