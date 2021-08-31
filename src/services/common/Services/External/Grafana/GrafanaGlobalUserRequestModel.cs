// <copyright file="GrafanaGlobalUserRequestModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.Common.Services.External.Grafana
{
    public class GrafanaGlobalUserRequestModel
    {
        public GrafanaGlobalUserRequestModel(string name, string email, string loginName, string password)
        {
            this.Name = name;
            this.Email = email;
            this.Login = loginName;
            this.Password = password;
        }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Login { get; set; }

        public string Password { get; set; }
    }
}