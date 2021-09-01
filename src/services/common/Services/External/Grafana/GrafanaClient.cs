// <copyright file="GrafanaClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.KeyVault;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Http;
using Mmm.Iot.Common.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.Common.Services.External.Grafana
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
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("admin:admin"));
                HttpRequest request = this.PrepareRequest(
                    "api/auth/keys",
                    svcCredentials,
                    "BasicAuth");

                GrafanaAPIKeyRequestModel requestData = new GrafanaAPIKeyRequestModel("adminAPIKey", GrafanaRoleType.Admin);

                request.SetContent(requestData);

                var response = await this.httpClient.PostAsync(request);
                var result = JsonConvert.DeserializeObject<GrafanaAPIKeyResponseModel>(response.Content);

                await this.keyVaultClient.SetValueAsync("Grafana--APIKey", result.Key);
            }
        }

        public async Task<string> CreateAPIKey(string orgId)
        {
            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("admin:admin"));
            HttpRequest request = this.PrepareRequest(
                "api/auth/keys",
                svcCredentials,
                "BasicAuth");

            GrafanaAPIKeyRequestModel requestData = new GrafanaAPIKeyRequestModel("adminAPIKey", GrafanaRoleType.Admin);

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
            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("admin:admin"));
            HttpRequest request = this.PrepareRequest(
                "api/admin/users",
                svcCredentials,
                "BasicAuth");
            request.SetContent(user);
            await this.httpClient.PostAsync(request);
        }

        public async Task DeleteUser(string userName, string apiKey)
        {
            HttpRequest request = this.PrepareRequest(
                $"api/org/users/lookup",
                apiKey,
                "Token");

            var response = await this.httpClient.GetAsync(request);

            List<GrafanaUserModel> result = JsonConvert.DeserializeObject<List<GrafanaUserModel>>(response.Content);

            GrafanaUserModel userDetails = result.FirstOrDefault(x => x.Login == userName);

            if (userDetails != null)
            {
                HttpRequest deleteRequest = this.PrepareRequest(
                    $"api/org/users/{userDetails.UserId}",
                    apiKey,
                    "Token");

                await this.httpClient.DeleteAsync(deleteRequest);
            }
        }

        public async Task AddUserToOrg(string userLoginName, GrafanaRoleType role, string apiKey)
        {
            HttpRequest request = this.PrepareRequest(
                "api/org/users",
                apiKey,
                "Token");

            var requestContent = new { Role = role.ToString(), LoginOrEmail = userLoginName };
            request.SetContent(requestContent);
            await this.httpClient.PostAsync(request);
        }

        public async Task<string> CreateOrganization(string tenant)
        {
            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("admin:admin"));
            HttpRequest request = this.PrepareRequest(
                "api/orgs",
                svcCredentials,
                "BasicAuth");

            GrafanaOrganizationRequestModel requestData = new GrafanaOrganizationRequestModel(tenant);
            request.SetContent(requestData);

            var response = await this.httpClient.PostAsync(request);

            dynamic result = JsonConvert.DeserializeObject(response.Content);
            return result?.orgId;
        }

        public async Task<GrafanaDashboardResponseModel> CreateAndUpdateDashboard(string dashboardTemplate, string apiKey)
        {
            HttpRequest request = this.PrepareRequest(
                "api/dashboards/db",
                apiKey,
                "Token");

            request.SetContent(dashboardTemplate);

            var response = await this.httpClient.PostAsync(request);
            return JsonConvert.DeserializeObject<GrafanaDashboardResponseModel>(response.Content);
        }

        public async Task DeleteDashboardByUid(string dashboardUid, string apiKey)
        {
            HttpRequest request = this.PrepareRequest(
                $"api/dashboards/uid/{dashboardUid}",
                apiKey,
                "Token");

            await this.httpClient.DeleteAsync(request);
        }

        public async Task<bool> DeleteOrganizationByUid(string orgId)
        {
            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("admin:admin"));
            HttpRequest request = this.PrepareRequest(
                $"api/orgs/{orgId}",
                svcCredentials,
                "BasicAuth");

            var result = await this.httpClient.DeleteAsync(request);
            return result.IsSuccess;
        }

        public async Task AddDataSource(string dataSourceTempete, string apiKey)
        {
            HttpRequest request = this.PrepareRequest(
                "api/datasources",
                apiKey,
                "Token");

            request.SetContent(dataSourceTempete);
            await this.httpClient.PostAsync(request);
        }

        private HttpRequest PrepareRequest(
           string path,
           string accessToken,
           string type)
        {
            Uri uri = new UriBuilder($"{this.serviceUri}/{path}").Uri;
            HttpRequest request = new HttpRequest(uri);
            request.Headers.Add("Accept", "application/json");

            switch (type)
            {
                case "BasicAuth":
                    request.Headers.Add("Authorization", "Basic " + accessToken);
                    break;
                case "Token":
                    request.Headers.Add("Authorization", "Bearer " + accessToken);
                    break;
                default:
                    break;
            }

            return request;
        }
    }
}