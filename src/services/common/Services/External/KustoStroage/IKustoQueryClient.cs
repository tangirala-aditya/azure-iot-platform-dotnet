// <copyright file="IKustoQueryClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.External.TimeSeries;

namespace Mmm.Iot.Common.Services.External.KustoStorage
{
    public interface IKustoQueryClient : IStatusOperation
    {
        Task<MessageList> QueryAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] deviceIds);

        Task<MessageList> QueryEventsAsync(
            int limit,
            string deviceId);
    }
}