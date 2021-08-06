// <copyright file="IKeyVaultClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mmm.Iot.Common.Services.External.KeyVault
{
    public interface IKeyVaultClient
    {
        Task SetValueAsync(string key, string value);

        string GetValue(string key);
    }
}