// <copyright file="TeamKbMapping.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.Models
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// this is mapping table entity of channel with knowledgebase.
    /// </summary>
    public class TeamKbMapping : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamKbMapping"/> class.
        /// Your entity type must expose a parameter-less constructor.
        /// </summary>
        public TeamKbMapping()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamKbMapping"/> class.
        /// </summary>
        /// <param name="partitionKey">Partition key.</param>
        /// <param name="rowKey">Row key.</param>
        public TeamKbMapping(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey; // constant
            this.RowKey = rowKey; // team id
        }

        /// <summary>
        /// Gets or sets KbId.
        /// </summary>
        public string KbId { get; set; }

        /// <summary>
        /// Gets or sets EndpointKey.
        /// </summary>
        public string EndpointKey { get; set; }
    }
}
