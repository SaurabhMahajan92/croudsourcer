// <copyright file="Constants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer
{
    /// <summary>
    /// constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// this is the qnamaker api endpoint header required for crud operations.
        /// </summary>
        public const string EndpointKeyHeader = "Ocp-Apim-Subscription-Key";

        /// <summary>
        /// Source.
        /// </summary>
        public const string Source = "Editorial";

        /// <summary>
        /// Dummy question.
        /// </summary>
        public const string DummyQuestion = "dummyquestion";

        /// <summary>
        /// Dummy answer.
        /// </summary>
        public const string DummyAnswer = "dummyanswer";

        /// <summary>
        /// Knowledgebase name.
        /// </summary>
        public const string KbName = "teamscrowdsourcer";

        /// <summary>
        /// Constant value used as a partition key in storage.
        /// </summary>
        public const string MsTeams = "msteams";

        /// <summary>
        /// Unanswered name.
        /// </summary>
        public const string Unanswered = "#$unanswered$#";

        /// <summary>
        /// Action name.
        /// </summary>
        public const string SubmitAddCommand = "submit/add";

        /// <summary>
        /// save command.
        /// </summary>
        public const string SaveCommand = "save";

        /// <summary>
        /// delete command.
        /// </summary>
        public const string DeleteCommand = "delete";

        /// <summary>
        /// no command.
        /// </summary>
        public const string NoCommand = "no";

        /// <summary>
        /// Default locale to use if client locale is missing.
        /// </summary>
        public const string DefaultLocale = "en-US";

        /// <summary>
        /// Table name which stores kbid and endpoint key for each teamid.
        /// </summary>
        public const string CrowdSourcerTableName = "crowdsourcer";

        /// <summary>
        /// add command text.
        /// </summary>
        public const string AddCommand = "add question";

        /// <summary>
        /// qna metadata team id name.
        /// </summary>
        public const string MetadataTeamId = "teamid";

        /// <summary>
        /// qna metadata createdat name.
        /// </summary>
        public const string MetadataCreatedAt = "createdat";

        /// <summary>
        /// qna metadata createdby name.
        /// </summary>
        public const string MetadataCreatedBy = "createdby";

        /// <summary>
        /// qna metadata conversationid name.
        /// </summary>
        public const string MetadataConversationId = "conversationid";

        /// <summary>
        /// qna metadata updatedat name.
        /// </summary>
        public const string MetadataUpdatedAt = "updatedat";

        /// <summary>
        /// qna metadata updatedby name.
        /// </summary>
        public const string MetadataUpdatedBy = "updatedby";

        /// <summary>
        /// MessagingExtension recently created command id.
        /// </summary>
        public const string CreatedCommandId = "created";

        /// <summary>
        /// MessagingExtension recently edited command id.
        /// </summary>
        public const string EditedCommandId = "edited";

        /// <summary>
        /// MessagingExtension unanswered command id.
        /// </summary>
        public const string UnAnsweredCommandId = "unanswered";
    }
}
