// <copyright file="IKustoTableManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Kusto.Data.Common;

namespace Mmm.Iot.Common.Services.External.KustoStorage
{
    public interface IKustoTableManagementClient
    {
        void CreateTable(string tableName, IEnumerable<Tuple<string, string>> rowFields, string databaseName);

        void CreateTableMapping(string tableMappingName, IEnumerable<ColumnMapping> mapping, string tableName, string databaseName);

        void EnableStreamingIngestionPolicyToTable(string tableName, string databaseName);
    }
}