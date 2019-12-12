// <copyright file="IQnaServiceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
    using Microsoft.Teams.Apps.CrowdSourcer.Models;

    /// <summary>
    /// qna maker service provider interface.
    /// </summary>
    public interface IQnaServiceProvider
    {
        /// <summary>
        /// this method is used to add QnA pair in Kb.
        /// </summary>
        /// <param name="question">question text.</param>
        /// <param name="answer">answer text.</param>
        /// <param name="createdBy">created by user.</param>
        /// <param name="teamId">team id.</param>
        /// <param name="conversationId">conversation id.</param>
        /// <returns>task.</returns>
        Task AddQnaAsync(string question, string answer, string createdBy, string teamId, string conversationId);

        /// <summary>
        /// this method is used to create a teamId-KbId mapping entry in storage.
        /// </summary>
        /// <param name="teamId">team id.</param>
        /// <param name="knowledgeBaseId">kb id.</param>
        /// <param name="endpointKey">endpoint key.</param>
        /// <returns>boolean result.</returns>
        Task<bool> CreateKbMappingAsync(string teamId, string knowledgeBaseId, string endpointKey);

        /// <summary>
        /// this method is used to create knowledgebase.
        /// </summary>
        /// <returns>kb id.</returns>
        Task<string> CreateKnowledgeBaseAsync();

        /// <summary>
        /// this method is used to delete Qna pair from KB.
        /// </summary>
        /// <param name="questionId">question id.</param>
        /// <param name="teamId">team id.</param>
        /// <returns>delete response.</returns>
        Task DeleteQnaAsync(int? questionId, string teamId);

        /// <summary>
        /// this method downloads all the qnadocuments from kb.
        /// </summary>
        /// <param name="teamId">team id.</param>
        /// <param name="answeredFlag">answered flag.</param>
        /// <param name="searchQuery">serach text.</param>
        /// <returns>all qnadocuments based on team id.</returns>
        Task<IEnumerable<QnADTO>> DownloadKnowledgebaseAsync(string teamId, bool answeredFlag, string searchQuery);

        /// <summary>
        /// get answer from kb for a given question.
        /// </summary>
        /// <param name="isTest">prod or test.</param>
        /// <param name="question">question text.</param>
        /// <param name="teamId">team id.</param>
        /// <returns>qnaSearchResult response.</returns>
        Task<QnASearchResultList> GenerateAnswerAsync(bool isTest, string question, string teamId);

        /// <summary>
        /// get kb details based on teamId.
        /// </summary>
        /// <param name="teamId">team id.</param>
        /// <returns>kb mapping.</returns>
        Task<TeamKbMapping> GetKbMappingAsync(string teamId);

        /// <summary>
        /// gets all the KB mappings from storage.
        /// </summary>
        /// <returns>all kb mappings.</returns>
        Task<IList<TeamKbMapping>> GetAllKbMappingsAsync();

        /// <summary>
        /// this method can be used to get endpoint keys of qnamaker service.
        /// </summary>
        /// <returns>primary endpoint key.</returns>
        Task<string> GetQnaMakerEndpointKeysAsync();

        /// <summary>
        /// this method can be used to publish the Kb.
        /// </summary>
        /// <param name="kbId">kb id.</param>
        /// <returns>task.</returns>
        Task PublishQnaAsync(string kbId);

        /// <summary>
        /// this method is used to update Qna pair in Kb.
        /// </summary>
        /// <param name="questionId">question id.</param>
        /// <param name="answer">answer text.</param>
        /// <param name="updatedBy">updated by user.</param>
        /// <param name="updatedQuestion">updated question text.</param>
        /// <param name="question">original question text.</param>
        /// <param name="teamId">team id.</param>
        /// <returns>task.</returns>
        Task UpdateQnaAsync(int? questionId, string answer, string updatedBy, string updatedQuestion, string question, string teamId);
    }
}