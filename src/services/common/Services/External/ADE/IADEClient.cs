// <copyright file="IADEClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.External.TimeSeries;

namespace Mmm.Iot.Common.Services.External.ADE
{
    public interface IADEClient : IStatusOperation
    {
        Task<MessageList> QueryADEDb(
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