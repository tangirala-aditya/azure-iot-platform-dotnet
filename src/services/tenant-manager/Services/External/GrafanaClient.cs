// <copyright file="GrafanaClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.KeyVault;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Http;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.TenantManager.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.TenantManager.Services.External
{
    public class GrafanaClient : IGrafanaClient
    {
        private readonly IExternalRequestHelper requestHelper;
        private readonly string serviceUri;
        private readonly IHttpClient httpClient;
        private IKeyVaultClient keyVaultClient;

        public GrafanaClient(AppConfig config, IExternalRequestHelper requestHelper, IKeyVaultClient keyVaultClient, IHttpClient httpClient)
        {
            this.serviceUri = config.Global.ExternalDependencies.GrafanaUrl;
            this.requestHelper = requestHelper;
            this.keyVaultClient = keyVaultClient;
            this.httpClient = httpClient;
        }

        public string RequestUrl(string path)
        {
            return $"{this.serviceUri}/{path}";
        }

        public async Task<StatusResultServiceModel> StatusAsync()
        {
            try
            {
                string url = this.RequestUrl("api/health");

                HttpRequest request = new HttpRequest(url);
                request.Headers.Add("Accept", "application/json");

                var response = await this.httpClient.GetAsync(request);

                if (response == null || response.IsError)
                {
                    // bad status
                    return new StatusResultServiceModel(false, "Not Healthy");
                }
                else
                {
                    return new StatusResultServiceModel(true, "Alive and well!");
                }
            }
            catch (JsonReaderException)
            {
                return new StatusResultServiceModel(false, $"Unable to read the response from the Grafana Status. The service may be down.");
            }
            catch (Exception e)
            {
                return new StatusResultServiceModel(false, $"Unable to get Grafana Status: {e.Message}");
            }
        }

        public async Task CreateAPIKeyIfNotFound()
        {
            string value = string.Empty;
            try
            {
                value = this.keyVaultClient.GetValue("Grafana--APIKey");
            }
            catch (Exception)
            {
            }

            if (string.IsNullOrEmpty(value))
            {
                string url = this.RequestUrl("api/auth/keys");
                UriBuilder uriBuilder = new UriBuilder(url);
                uriBuilder.UserName = "admin";
                uriBuilder.Password = "admin";

                GrafanaAPIKeyRequestModel requestData = new GrafanaAPIKeyRequestModel("adminAPIKey", GrafanaRoleType.Admin);

                HttpRequest request = new HttpRequest(uriBuilder.Uri);
                request.Headers.Add("Accept", "application/json");

                request.SetContent(requestData);

                var response = await this.httpClient.PostAsync(request);
                var result = JsonConvert.DeserializeObject<GrafanaAPIKeyResponseModel>(response.Content);

                await this.keyVaultClient.SetValueAsync("Grafana--APIKey", result.Key);
            }
        }

        public async Task<string> CreateAPIKey(string orgId)
        {
            string url = this.RequestUrl("api/auth/keys");
            UriBuilder uriBuilder = new UriBuilder(url);
            uriBuilder.UserName = "admin";
            uriBuilder.Password = "admin";

            GrafanaAPIKeyRequestModel requestData = new GrafanaAPIKeyRequestModel("adminAPIKey", GrafanaRoleType.Admin);

            HttpRequest request = new HttpRequest(uriBuilder.Uri);
            request.Headers.Add("Accept", "application/json");

            if (!string.IsNullOrEmpty(orgId))
            {
                request.Headers.Add("X-Grafana-Org-Id", orgId);
            }

            request.SetContent(requestData);

            var response = await this.httpClient.PostAsync(request);
            var result = JsonConvert.DeserializeObject<GrafanaAPIKeyResponseModel>(response.Content);

            return result.Key;
        }

        public async Task AddGlobalUser(GrafanaGlobalUserRequestModel user)
        {
            string url = this.RequestUrl("api/admin/users");
            UriBuilder uriBuilder = new UriBuilder(url);
            uriBuilder.UserName = "admin";
            uriBuilder.Password = "admin";

            HttpRequest request = new HttpRequest(uriBuilder.Uri);
            request.Headers.Add("Accept", "application/json");
            request.SetContent(user);

            await this.httpClient.PostAsync(request);
        }

        public async Task AddUserToOrg(string userLoginName, GrafanaRoleType role, string apiKey)
        {
            HttpRequest request = this.PrepareRequest(
                "api/org/users",
                apiKey);
            request.Headers.Add("Accept", "application/json");
            var requestContent = new { Role = role.ToString(), LoginOrEmail = userLoginName };
            request.SetContent(requestContent);

            await this.httpClient.PostAsync(request);
        }

        public async Task<string> CreateOrganization(string tenant)
        {
            string url = this.RequestUrl("/api/orgs");
            UriBuilder uriBuilder = new UriBuilder(url);
            uriBuilder.UserName = "admin";
            uriBuilder.Password = "admin";

            GrafanaOrganizationRequestModel requestData = new GrafanaOrganizationRequestModel($"Tenant-{tenant}");

            HttpRequest request = new HttpRequest(uriBuilder.Uri);
            request.Headers.Add("Accept", "application/json");
            request.SetContent(requestData);

            var response = await this.httpClient.PostAsync(request);

            dynamic result = JsonConvert.DeserializeObject(response.Content);

            return result.OrgId as string;
        }

        public async Task<GrafanaDashboardResponseModel> CreateAndUpdateDashboard(string dashboardTemplate, string apiKey)
        {
            HttpRequest request = this.PrepareRequest(
                "api/dashboards/db",
                apiKey);

            request.SetContent(dashboardTemplate);

            var response = await this.httpClient.PostAsync(request);
            return JsonConvert.DeserializeObject<GrafanaDashboardResponseModel>(response.Content);
        }

        public async Task DeleteDashboardByUid(string dashboardUid, string apiKey)
        {
            HttpRequest request = this.PrepareRequest(
                $"api/dashboards/uid/{dashboardUid}",
                apiKey);

            await this.httpClient.DeleteAsync(request);
        }

        public async Task DeleteOrganizationByUid(string orgId)
        {
            string url = this.RequestUrl($"/api/orgs/{orgId}");
            UriBuilder uriBuilder = new UriBuilder(url);
            uriBuilder.UserName = "admin";
            uriBuilder.Password = "admin";

            HttpRequest request = new HttpRequest(url);
            request.Headers.Add("Accept", "application/json");

            await this.httpClient.DeleteAsync(request);
        }

        public async Task AddDataSource(string dataSourceTempete, string apiKey)
        {
            HttpRequest request = this.PrepareRequest(
                "/api/datasources",
                apiKey);

            request.SetContent(dataSourceTempete);

            await this.httpClient.PostAsync(request);
        }

        private HttpRequest PrepareRequest(
           string path,
           string accessToken)
        {
            Uri uri = new UriBuilder($"{this.serviceUri}/{path}").Uri;
            HttpRequest request = new HttpRequest(uri);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            return request;
        }
    }
}