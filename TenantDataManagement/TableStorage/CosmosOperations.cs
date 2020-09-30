// <copyright file="CosmosOperations.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace TenantDataManagement
{
    public class CosmosOperations
    {
        private const string InvalidCharacterRegex = @"[^A-Za-z0-9:;.,_\-@]";
        private static CosmosOperations instance = null;
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly DocumentClient client = null;
        private RequestOptions requestOptions;

        public CosmosOperations()
        {
        }

        private CosmosOperations(DocumentClient client)
        {
            this.client = client;
        }

        public static async Task<CosmosOperations> GetClientAsync()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return instance ?? (instance = CreateInstance());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public static SqlQuerySpec GetDocumentsByCollectionId(string sqlQuery)
        {
            var queryBuilder = new StringBuilder(sqlQuery);
            return new SqlQuerySpec(queryBuilder.ToString());
        }

        public int QueryCountAsync(
            string databaseName,
            string colId,
            FeedOptions queryOptions,
            SqlQuerySpec querySpec)
        {
            if (queryOptions == null)
            {
                queryOptions = new FeedOptions
                {
                    EnableCrossPartitionQuery = false,
                    EnableScanInQuery = true,
                };
            }

            string collectionLink = string.Format(
                "/dbs/{0}/colls/{1}",
                databaseName,
                colId);

            try
            {
                var result = this.client.CreateDocumentQuery(
                    collectionLink,
                    querySpec,
                    queryOptions);

                var resultList = result == null ? new Document[0] : result.ToArray();

                return resultList.Length > 0 ? (int)resultList[0] : 0;
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is DocumentClientException)
                {
                    throw this.ConvertDocumentClientException(ae.InnerException as DocumentClientException);
                }
                else
                {
                    throw;
                }
            }
            catch (DocumentClientException dce)
            {
                throw this.ConvertDocumentClientException(dce);
            }
        }

        public string QueryValueAsync(
            string databaseName,
            string colId,
            FeedOptions queryOptions,
            SqlQuerySpec querySpec)
        {
            if (queryOptions == null)
            {
                queryOptions = new FeedOptions
                {
                    EnableCrossPartitionQuery = false,
                    EnableScanInQuery = true,
                };
            }

            string collectionLink = string.Format(
                "/dbs/{0}/colls/{1}",
                databaseName,
                colId);

            try
            {
                var result = this.client.CreateDocumentQuery(
                    collectionLink,
                    querySpec,
                    queryOptions);

                var resultList = result == null ? new Document[0] : result.ToArray();

                return resultList.Length > 0 ? (string)resultList[0] : string.Empty;
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is DocumentClientException)
                {
                    throw this.ConvertDocumentClientException(ae.InnerException as DocumentClientException);
                }
                else
                {
                    throw;
                }
            }
            catch (DocumentClientException dce)
            {
                throw this.ConvertDocumentClientException(dce);
            }
        }

        public async Task<bool> CheckCollectionIfExistsAsync(string cosmosDatabase, string cosmosColection, int docDBRUs)
        {
            bool status = false;
            try
            {
                this.requestOptions = this.GetDocDbOptions(docDBRUs);
                var uri = $"/dbs/{cosmosDatabase}/colls/{cosmosColection}";
                await this.client.ReadDocumentCollectionAsync(uri, this.requestOptions);
                status = true;
            }
            catch (DocumentClientException)
            {
                status = false;
            }

            return status;
        }

        private static CosmosOperations CreateInstance()
        {
            string connectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString", EnvironmentVariableTarget.Process);
            DocumentClient docClient = CreateClient(connectionString);
            return new CosmosOperations(docClient);
        }

        private static DocumentClient CreateClient(string docDBConnectionString)
        {
            string connectionStringRegex = "^AccountEndpoint=(?<endpoint>.*);AccountKey=(?<key>.*);$";
            try
            {
                var match = Regex.Match(docDBConnectionString, connectionStringRegex);
                if (!match.Success)
                {
                    string message = "Invalid Connection String for CosmosDb";
                    throw new Exception(message);
                }

                Uri docDbEndpoint = new Uri(match.Groups["endpoint"].Value);
                string docDbKey = match.Groups["key"].Value;
                return new DocumentClient(
                    docDbEndpoint,
                    docDbKey,
                    new ConnectionPolicy
                    {
                        ConnectionMode = ConnectionMode.Direct,
                        ConnectionProtocol = Protocol.Tcp,
                    });
            }
            catch (Exception ex)
            {
                throw new ApplicationException("DocumentClient creation failed in the helper class", ex);
            }
        }

        private RequestOptions GetDocDbOptions(int docDBRUs)
        {
            return new RequestOptions
            {
                OfferThroughput = docDBRUs,
                ConsistencyLevel = ConsistencyLevel.Strong,
            };
        }

        private Exception ConvertDocumentClientException(DocumentClientException e)
        {
            return e;
        }
    }
}