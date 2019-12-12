// <copyright file="KnowledgeBaseStorage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.AzureFunction
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Model for getting details from table storage.
    /// </summary>
    public class KnowledgeBaseStorage : TableEntity
    {
        /// <summary>
        /// Gets or sets Endpoint Key.
        /// </summary>
        public string EndpointKey { get; set; }

        /// <summary>
        /// Gets or sets KbId.
        /// </summary>
        public string KbId { get; set; }
    }
}
