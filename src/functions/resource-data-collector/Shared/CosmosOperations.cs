using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace ResourceDataCollector.Shared
{
    public class CosmosOperations
    {
        private static CosmosOperations instance = null;
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly DocumentClient client = null;

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


        public static SqlQuerySpec GetLatestDocumentsSql(
            int limit)
        {

            var sqlParameterCollection = new SqlParameterCollection();
            sqlParameterCollection.Add(new SqlParameter { Name = "@top", Value = limit });
            var queryBuilder = new StringBuilder("SELECT TOP @top * FROM c");

            queryBuilder.Append(" ORDER BY c[\"_ts\"] DESC");
            return new SqlQuerySpec(queryBuilder.ToString(), sqlParameterCollection);
        }

        public async Task<List<Document>> QueryAllDocumentsAsync(
            string databaseName,
            string colId,
            FeedOptions queryOptions,
            SqlQuerySpec querySpec)
        {
            if (queryOptions == null)
            {
                queryOptions = new FeedOptions
                {
                    EnableCrossPartitionQuery = true,
                    EnableScanInQuery = true,
                };
            }

            string collectionLink = string.Format(
                "/dbs/{0}/colls/{1}",
                databaseName,
                colId);

            try
            {
                var result = await Task.FromResult(this.client.CreateDocumentQuery<Document>(
                        collectionLink,
                        querySpec,
                        queryOptions));

                var queryResults = result == null ?
                    new List<Document>() :
                    result
                        .AsEnumerable()
                        .ToList();

                return queryResults;
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

        private Exception ConvertDocumentClientException(DocumentClientException e)
        {
            switch (e.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    return new ResourceNotFoundException(e.Message, e);
                case HttpStatusCode.Conflict:
                    return new ConflictingResourceException(e.Message, e);
                default:
                    return e;
            }
        }
    }
}
