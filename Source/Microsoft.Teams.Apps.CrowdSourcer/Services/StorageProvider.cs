// <copyright file="StorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CrowdSourcer.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Storage provider to use azure table storage.
    /// </summary>
    public class StorageProvider : IStorageProvider
    {
        private static CloudTableClient cloudTableClient;
        private static CloudTable configurationCloudTable;
        private readonly Lazy<Task> initializeTask;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageProvider"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration settings.</param>
        public StorageProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(this.configuration["StorageConnectionString"]));
        }

        /// <summary>
        /// Insert or merge the table entity.
        /// </summary>
        /// <typeparam name="T">storage table entity.</typeparam>
        /// <param name="entity">table entity.</param>
        /// <returns>table entity inserted or updated.</returns>
        public async Task<T> InsertOrMergeTableEntityAsync<T>(T entity)
            where T : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException("null table entity");
            }

            await this.EnsureInitializedAsync();
            TableOperation insertOrMergeOperation = TableOperation.InsertOrReplace(entity);
            TableResult result = await configurationCloudTable.ExecuteAsync(insertOrMergeOperation);
            return result.Result as T;
        }

        /// <summary>
        /// Execute asynchronously the most efficient storage query - the point query - where both partition key and row key are specified.
        /// </summary>
        /// <typeparam name="T">storage table entity.</typeparam>
        /// <param name="partitionKey">partition key of the table.</param>
        /// <param name="rowKey">row key of the table.</param>
        /// <returns>entity.</returns>
        public async Task<T> ExecuteQueryUsingPointQueryAsync<T>(string partitionKey, string rowKey)
            where T : TableEntity
        {
            await this.EnsureInitializedAsync();
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            TableResult result = await configurationCloudTable.ExecuteAsync(retrieveOperation);
            var entity = (T)result?.Result;
            return entity;
        }

        /// <summary>
        /// get kb mapping details.
        /// </summary>
        /// <returns>TeamKbMappingResponse.</returns>
        public async Task<List<TeamKbMapping>> GetKbMappingsAsync()
        {
            await this.EnsureInitializedAsync();
            TableQuery<TeamKbMapping> query = new TableQuery<TeamKbMapping>();
            TableContinuationToken token = null;
            var entities = new List<TeamKbMapping>();
            do
            {
                var queryResult = await configurationCloudTable.ExecuteQuerySegmentedAsync(query, token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            }
            while (token != null);

            return entities;
        }

        /// <summary>
        /// Create teams table if it doesnt exists.
        /// </summary>
        /// <param name="connectionString">storage account connection string.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation task which represents table is created if its not existing.</returns>
        private async Task InitializeAsync(string connectionString)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            cloudTableClient = storageAccount.CreateCloudTableClient();
            configurationCloudTable = cloudTableClient.GetTableReference(Constants.CrowdSourcerTableName);
            if (!await configurationCloudTable.ExistsAsync())
            {
                await configurationCloudTable.CreateIfNotExistsAsync();
            }
        }

        /// <summary>
        /// Initialization of InitializeAsync method which will help in creating table.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task EnsureInitializedAsync()
        {
            await this.initializeTask.Value;
        }
    }
}
