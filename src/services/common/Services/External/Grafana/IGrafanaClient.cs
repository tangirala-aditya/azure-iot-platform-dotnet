// <copyright file="IGrafanaClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Mmm.Iot.Common.Services.External.Grafana
{
    public interface IGrafanaClient : IExternalServiceClient
    {
        Task CreateAPIKeyIfNotFound();

        Task<string> CreateAPIKey(string orgId);

        Task AddGlobalUser(GrafanaGlobalUserRequestModel user);

        Task AddUserToOrg(string userLoginName, GrafanaRoleType role, string apiKey);

        Task<string> CreateOrganization(string tenant);

        Task<GrafanaDashboardResponseModel> CreateAndUpdateDashboard(string dashboardTemplate, string apikey);

        Task DeleteDashboardByUid(string dashboardUid, string apikey);

        Task<bool> DeleteOrganizationByUid(string orgId);

        Task AddDataSource(string dataSourceTempete, string apiKey);

        Task DeleteUser(string userName, string apiKey);
    }
}