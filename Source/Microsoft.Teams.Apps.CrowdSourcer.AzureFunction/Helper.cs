// <copyright file="Helper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.AzureFunction
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
    using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Helper class for fetching details from table storage.
    /// </summary>
    internal class Helper
    {
        /// <summary>
        /// Table name for fetching knowledge base ID from table storage.
        /// </summary>
        private const string CrowdSourcerTableName = "crowdsourcer";

        /// <summary>
        /// Partition key for fetching knowledge base ID from table storage.
        /// </summary>
        private const string PartitionKey = "msteams";

        /// <summary>
        /// Header for endpoint.
        /// </summary>
        private const string EndpointKeyHeader = "Ocp-Apim-Subscription-Key";

        /// <summary>
        /// QnAMaker client.
        /// </summary>
        private IQnAMakerClient qnaMakerClient = new QnAMakerClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("QnaSubscriptionKey"))) { Endpoint = Environment.GetEnvironmentVariable("QnAMakerHostUrl") };

        /// <summary>
        /// Get list of distinct knowledgebase ID from table storage.
        /// </summary>
        /// <returns>List of knowledgebase ID.</returns>
        public async Task<List<string>> GetAllKnowledgeBaseIdsAsync()
        {
            CloudTable cloudTable = await this.GetOrCreateTableAsync();
            List<KnowledgeBaseStorage> result = new List<KnowledgeBaseStorage>();
            TableQuery<KnowledgeBaseStorage> query =
                new TableQuery<KnowledgeBaseStorage>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey));
            TableContinuationToken tableContinuationToken = null;
            do
            {
                TableQuerySegment<KnowledgeBaseStorage> queryResponse = await cloudTable.ExecuteQuerySegmentedAsync(query, tableContinuationToken);
                tableContinuationToken = queryResponse.ContinuationToken;
                result.AddRange(queryResponse.Results);
            }
            while (tableContinuationToken != null);
            return result.Select(x => x.KbId).Distinct().ToList();
        }

        /// <summary>
        /// Checks whether knowledge base need to be published.
        /// </summary>
        /// <param name="kbId">Knowledgebase ID.</param>
        /// <returns>boolean variable to publish or not.</returns>
        public async Task<bool> GetPublishStatusAsync(string kbId)
        {
            KnowledgebaseDTO qnaDocuments = await this.qnaMakerClient.Knowledgebase.GetDetailsAsync(kbId);
            if (qnaDocuments != null && qnaDocuments.LastChangedTimestamp != null && qnaDocuments.LastPublishedTimestamp != null)
            {
                return DateTime.Compare(Convert.ToDateTime(qnaDocuments.LastChangedTimestamp), Convert.ToDateTime(qnaDocuments.LastPublishedTimestamp)) > 0;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Method is used to publish knowledgebase.
        /// </summary>
        /// <param name="kbId">Knowledgebase Id.</param>
        /// <returns>Task.</returns>
        public async Task PublishAsync(string kbId)
        {
            await this.qnaMakerClient.Knowledgebase.PublishAsync(kbId);
        }

        private async Task<CloudTable> GetOrCreateTableAsync()
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            CloudTableClient tableClient = cloudStorageAccount.CreateCloudTableClient();
            CloudTable cloudTable = tableClient.GetTableReference(CrowdSourcerTableName);
            bool res = await cloudTable.CreateIfNotExistsAsync();
            return cloudTable;
        }
    }
}
