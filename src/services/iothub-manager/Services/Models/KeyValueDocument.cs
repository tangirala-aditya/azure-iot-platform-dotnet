// <copyright file="KeyValueDocument.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Documents;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    internal sealed class KeyValueDocument : Resource
    {
        public KeyValueDocument(string collectionId, string key, string data, string id)
        {
            this.Id = id;
            this.CollectionId = collectionId;
            this.Key = key;
            this.Data = data;
        }

        public string CollectionId { get; }

        public string Key { get; }

        public string Data { get; }
    }
}