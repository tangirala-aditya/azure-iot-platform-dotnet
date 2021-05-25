using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace ResourceDataCollector.Shared
{
    public class TenantService
    {
        private FeedOptions DefaultQueryOptions => new FeedOptions
        {
            EnableCrossPartitionQuery = true,
            EnableScanInQuery = true,
        };

        public async Task<List<Document>> GetDocuments(string tenantId)
        {
            var sql = CosmosOperations.GetLatestDocumentsSql(1);

            List<Document> docs = new List<Document>();

            try
            {
                CosmosOperations storageClient = await CosmosOperations.GetClientAsync();
                docs = await storageClient.QueryAllDocumentsAsync(
                   "iot",
                   $"telemetry-{tenantId}",
                   this.DefaultQueryOptions,
                   sql);

                var result = docs == null ?
                    new List<Document>() :
                    docs;

                return result;
            }
            catch (ResourceNotFoundException e)
            {
                return new List<Document>();
            }
        }
    }
}
