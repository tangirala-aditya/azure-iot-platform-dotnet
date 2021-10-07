// <copyright file="KustoQueryClientTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.KustoStorage;
using Mmm.Iot.Common.Services.External.KustoStroage;
using Mmm.Iot.Common.TestHelpers;
using Moq;
using Xunit;

namespace Mmm.Iot.Common.Services.Test
{
    public class KustoQueryClientTest
    {
        private const string DatabaseName = "testDatabase";
        private readonly Mock<AppConfig> mockConfig;
        private readonly IKustoQueryClient client;
        private readonly Random rand;

        // KustoQueryClient specific variables
        private readonly Mock<ILogger<KustoQueryClient>> mockILogger;
        private readonly Mock<ICslQueryProvider> mockKustoQueryClient;

        public KustoQueryClientTest()
        {
            this.mockILogger = new Mock<ILogger<KustoQueryClient>>();
            this.mockKustoQueryClient = new Mock<ICslQueryProvider>();
            this.rand = new Random();

            this.mockConfig = new Mock<AppConfig>();
            this.mockConfig
                .Setup(x => x.Global.AzureActiveDirectory.AppId)
                .Returns(this.rand.NextString());
            this.mockConfig
                .Setup(x => x.Global.AzureActiveDirectory.AppSecret)
                .Returns(this.rand.NextString());
            this.mockConfig
                .Setup(x => x.Global.AzureActiveDirectory.TenantId)
                .Returns(this.rand.NextString());

            this.client = new KustoQueryClient(
                this.mockConfig.Object,
                this.mockILogger.Object,
                this.mockKustoQueryClient.Object);
        }

        [Fact]
        public async Task QueryAsyncSuccessTest()
        {
            // arrange
            var dataReader = new Mock<IDataReader>();
            var query = this.rand.NextString();
            var queryParameter = new Dictionary<string, string>();
            this.mockKustoQueryClient
                .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClientRequestProperties>()))
                .Returns(Task.FromResult(dataReader.Object));

            // act
            var result = await this.client.ExecuteQueryAsync<TelemetryModel>(DatabaseName, query, queryParameter);

            // assert that QueryAsync returns a document, document will be an empty Document Object
            Assert.NotNull(result);
            Assert.IsType<List<TelemetryModel>>(result);
        }
    }
}