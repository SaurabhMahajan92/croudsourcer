// <copyright file="PublishFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.AzureFunction
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;

    /// <summary>
    /// Azure Function to publish knowledge bases if modified.
    /// </summary>
    public static class PublishFunction
    {
        private static Helper helper = new Helper();

        /// <summary>
        /// Function to get list of KB and publish KB.
        /// </summary>
        /// <param name="myTimer">Duration of publish operations.</param>
        /// <param name="log">Log.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [FunctionName("PublishFunction")]
        public static async Task Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                List<string> knowledgeBaseIdList = await helper.GetAllKnowledgeBaseIdsAsync();
                foreach (string kb in knowledgeBaseIdList)
                {
                    bool toBePublished = await helper.GetPublishStatusAsync(kb);
                    log.Info("To be Published - " + toBePublished);
                    log.Info("KbId - " + kb);
                    log.Info("QnAMakerHostUrl - " + Environment.GetEnvironmentVariable("QnAMakerHostUrl"));
                    if (toBePublished)
                    {
                        await helper.PublishAsync(kb);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error: " + ex.Message); // Exception logging.
            }
        }
    }
}
