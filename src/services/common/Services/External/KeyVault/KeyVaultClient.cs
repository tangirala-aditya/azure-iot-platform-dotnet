// <copyright file="KeyVaultClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Mmm.Iot.Common.Services.Config;

namespace Mmm.Iot.Common.Services.External.KeyVault
{
    public class KeyVaultClient : IKeyVaultClient
    {
        private SecretClient client;

        public KeyVaultClient(AppConfig config)
        {
            var kvUri = $"https://{config.Global.KeyVault.Name}.vault.azure.net";
            this.client = new SecretClient(new Uri(kvUri), new ClientSecretCredential(
               config.Global.AzureActiveDirectory.TenantId,
               config.Global.AzureActiveDirectory.AppId,
               config.Global.AzureActiveDirectory.AppSecret));
        }

        public async Task SetValueAsync(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("the key parameter must not be null or empty to create a new Key Vault key value pair.");
            }

            try
            {
                await this.client.SetSecretAsync(key, value);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to create the Key Vault key value pair {{{key}, {value}}}", e);
            }
        }

        public string GetValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("Key Vault cannot take a null key parameter. The given key was not correctly configured.");
            }

            string value = string.Empty;
            try
            {
                KeyVaultSecret secret = this.client.GetSecret(key);
                value = secret.Value;
            }
            catch (Exception e)
            {
                throw new Exception($"An exception occured while getting the value of {key} from Key Vault:\n" + e.Message);
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new NullReferenceException($"Key Vault returned a null value for {key}");
            }

            return value;
        }
    }
}