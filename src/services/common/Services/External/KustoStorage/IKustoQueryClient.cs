// <copyright file="IKustoQueryClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mmm.Iot.Common.Services.External.KustoStorage
{
    public interface IKustoQueryClient
    {
        Task<List<TDestination>> QueryAsync<TDestination>(
            string dataBaseName,
            string query,
            Dictionary<string, string> queryParameter);
    }
}