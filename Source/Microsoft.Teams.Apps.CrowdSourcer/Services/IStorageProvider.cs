// <copyright file="IStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CrowdSourcer.Models;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// storage provider interface.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Insert or merge the table entity.
        /// </summary>
        /// <param name="entity">table entity.</param>
        /// <returns>table entity inserted or updated.</returns>
        Task<T> InsertOrMergeTableEntityAsync<T>(T entity)
            where T : TableEntity;

        /// <summary>
        /// Execute asynchronously the most efficient storage query - the point query - where both partition key and row key are specified.
        /// </summary>
        /// <param name="partitionKey">partition key of the table.</param>
        /// <param name="rowKey">row key of the table.</param>
        /// <returns>entity.</returns>
        Task<T> ExecuteQueryUsingPointQueryAsync<T>(string partitionKey, string rowKey)
            where T : TableEntity;

        /// <summary>
        /// get kb mapping details.
        /// </summary>
        /// <returns>TeamKbMappingResponse.</returns>
        Task<List<TeamKbMapping>> GetKbMappingsAsync();
    }
}