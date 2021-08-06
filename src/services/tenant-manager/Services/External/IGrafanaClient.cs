// <copyright file="IGrafanaClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Mmm.Iot.Common.Services.External;
using Mmm.Iot.TenantManager.Services.Models;

namespace Mmm.Iot.TenantManager.Services.External
{
    public interface IGrafanaClient : IExternalServiceClient
    {
        Task CreateAPIKeyIsNotFound();

        Task<GrafanaDashboardResponseModel> CreateAndUpdateDashboard(string dashboardTemplate);

        Task DeleteDashboardByUid(string dashboardUid);
    }
}