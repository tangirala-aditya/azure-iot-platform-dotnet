// <copyright file="GrafanaGlobalUserRequestModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.TenantManager.Services.Models
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