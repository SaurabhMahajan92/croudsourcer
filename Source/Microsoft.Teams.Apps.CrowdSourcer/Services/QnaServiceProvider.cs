// <copyright file="QnaServiceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.ApplicationInsights;
    using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
    using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CrowdSourcer.Models;

    /// <summary>
    /// qna maker service provider class.
    /// </summary>
    public class QnaServiceProvider : IQnaServiceProvider
    {
        private IQnAMakerClient qnaMakerClient;
        private IQnAMakerRuntimeClient qnaMakerRuntimeClient;
        private IConfiguration configuration;
        private IStorageProvider storageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="QnaServiceProvider"/> class.
        /// </summary>
        /// <param name="storageProvider">storage provider.</param>
        /// <param name="telemetryClient">telemetry client.</param>
        /// <param name="configuration">configuration.</param>
        public QnaServiceProvider(IStorageProvider storageProvider, IConfiguration configuration, TelemetryClient telemetryClient)
        {
            this.storageProvider = storageProvider;
            this.configuration = configuration;
            this.qnaMakerClient = new QnAMakerClient(new ApiKeyServiceClientCredentials(this.configuration["QnAMakerApiEndpointKey"])) { Endpoint = this.configuration["QnAMakerApiUrl"] };
        }

        /// <summary>
        /// this method is used to add QnA pair in Kb.
        /// </summary>
        /// <param name="question">question text.</param>
        /// <param name="answer">answer text.</param>
        /// <param name="createdBy">created by user.</param>
        /// <param name="teamId">team id.</param>
        /// <param name="conversationId">conversation id.</param>
        /// <returns>task.</returns>
        public async Task AddQnaAsync(string question, string answer, string createdBy, string teamId, string conversationId)
        {
            var kb = await this.GetKbMappingAsync(teamId);

            // Update kb
            var updateKbOperation = await this.qnaMakerClient.Knowledgebase.UpdateAsync(kb?.KbId, new UpdateKbOperationDTO
            {
                // Create JSON of changes.
                Add = new UpdateKbOperationDTOAdd
                {
                    QnaList = new List<QnADTO>
                    {
                         new QnADTO
                         {
                            Questions = new List<string> { question },
                            Answer = answer,
                            Metadata = new List<MetadataDTO>()
                            {
                                new MetadataDTO() { Name = Constants.MetadataCreatedAt, Value = DateTime.UtcNow.Ticks.ToString() },
                                new MetadataDTO() { Name = Constants.MetadataCreatedBy, Value = createdBy },
                                new MetadataDTO() { Name = Constants.MetadataTeamId, Value = HttpUtility.UrlEncode(teamId) },
                                new MetadataDTO() { Name = Constants.MetadataConversationId, Value = conversationId?.Split(':').Last() },
                            },
                         },
                    },
                },
            });
        }

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
        public async Task UpdateQnaAsync(int? questionId, string answer, string updatedBy, string updatedQuestion, string question, string teamId)
        {
            var kb = await this.GetKbMappingAsync(teamId);
            var questions = default(UpdateQnaDTOQuestions);
            if (!string.IsNullOrEmpty(updatedQuestion))
            {
                questions = (updatedQuestion == question) ? null : new UpdateQnaDTOQuestions() { Add = new List<string> { updatedQuestion }, Delete = new List<string> { question } };
            }

            if (string.IsNullOrEmpty(answer))
            {
                answer = Constants.Unanswered;
            }

            // Update kb
            var updateKbOperation = await this.qnaMakerClient.Knowledgebase.UpdateAsync(kb?.KbId, new UpdateKbOperationDTO
            {
                // Create JSON of changes.
                Update = new UpdateKbOperationDTOUpdate()
                {
                    QnaList = new List<UpdateQnaDTO>()
                    {
                        new UpdateQnaDTO()
                        {
                            Id = questionId,
                            Source = Constants.Source,
                            Answer = answer,
                            Questions = questions,
                            Metadata = new UpdateQnaDTOMetadata()
                            {
                                Add = new List<MetadataDTO>()
                                {
                                    new MetadataDTO() { Name = Constants.MetadataUpdatedAt, Value = DateTime.UtcNow.Ticks.ToString() },
                                    new MetadataDTO() { Name = Constants.MetadataUpdatedBy, Value = updatedBy },
                                },
                            },
                        },
                    },
                },
            });
        }

        /// <summary>
        /// this method is used to delete Qna pair from KB.
        /// </summary>
        /// <param name="questionId">question id.</param>
        /// <param name="teamId">team id.</param>
        /// <returns>task.</returns>
        public async Task DeleteQnaAsync(int? questionId, string teamId)
        {
            var kb = await this.GetKbMappingAsync(teamId);

            // to delete a qna based on id.
            var updateKbOperation = await this.qnaMakerClient.Knowledgebase.UpdateAsync(kb?.KbId, new UpdateKbOperationDTO
            {
                // Create JSON of changes.
                Delete = new UpdateKbOperationDTODelete()
                {
                    Ids = new List<int?>() { questionId },
                },
            });
        }

        /// <summary>
        /// get answer from kb for a given question.
        /// </summary>
        /// <param name="isTest">prod or test.</param>
        /// <param name="question">question text.</param>
        /// <param name="teamId">team id.</param>
        /// <returns>qnaSearchResult response.</returns>
        public async Task<QnASearchResultList> GenerateAnswerAsync(bool isTest, string question, string teamId)
        {
            var kb = await this.GetKbMappingAsync(teamId);

            this.qnaMakerRuntimeClient = new QnAMakerRuntimeClient(new EndpointKeyServiceClientCredentials(kb.EndpointKey)) { RuntimeEndpoint = this.configuration["QnAMakerHostUrl"] };

            QnASearchResultList qnaSearchResult = await this.qnaMakerRuntimeClient.Runtime.GenerateAnswerAsync(kb?.KbId, new QueryDTO()
            {
                IsTest = isTest,
                Question = question,
                ScoreThreshold = double.Parse(this.configuration["ScoreThreshold"]),
                StrictFilters = new List<MetadataDTO> { new MetadataDTO() { Name = Constants.MetadataTeamId, Value = HttpUtility.UrlEncode(teamId) } },
            });

            return qnaSearchResult;
        }

        /// <summary>
        /// this method downloads all the qnadocuments from kb and filters data according to teamid, search query and answered/unanswered questions.
        /// </summary>
        /// <param name="teamId">team id.</param>
        /// <param name="answeredFlag">flag to determine if only answered questions needs to be shown.</param>
        /// <param name="searchQuery">serach text.</param>
        /// <returns>all qnadocuments based on team id.</returns>
        public async Task<IEnumerable<QnADTO>> DownloadKnowledgebaseAsync(string teamId, bool answeredFlag, string searchQuery)
        {
            var kb = await this.GetKbMappingAsync(teamId);

            var qnaDocuments = await this.qnaMakerClient.Knowledgebase.DownloadAsync(kb.KbId, environment: "Prod");

            string splitTeamId = HttpUtility.UrlEncode(teamId);
            IEnumerable<QnADTO> items;
            if (string.IsNullOrEmpty(searchQuery))
            {
                items = answeredFlag ?
                        qnaDocuments.QnaDocuments
                            .Where(
                                qnaDocument => qnaDocument.Metadata.FirstOrDefault(metadata => metadata.Name == Constants.MetadataTeamId).Value == splitTeamId
                                && qnaDocument.Answer != Constants.Unanswered)
                        : qnaDocuments.QnaDocuments
                            .Where(
                                qnaDocument => qnaDocument.Metadata.FirstOrDefault(metadata => metadata.Name == Constants.MetadataTeamId).Value == splitTeamId
                                && qnaDocument.Answer == Constants.Unanswered);
            }
            else
            {
                items = answeredFlag ?
                       qnaDocuments.QnaDocuments
                           .Where(
                               qnaDocument => qnaDocument.Metadata.FirstOrDefault(metadata => metadata.Name == Constants.MetadataTeamId).Value == splitTeamId
                               && qnaDocument.Answer != Constants.Unanswered
                               && qnaDocument.Questions.Where(z => z.Contains(searchQuery)).Count() > 0)
                       : qnaDocuments.QnaDocuments
                           .Where(
                               qnaDocument => qnaDocument.Metadata.FirstOrDefault(metadata => metadata.Name == Constants.MetadataTeamId).Value == splitTeamId
                               && qnaDocument.Answer == Constants.Unanswered
                               && qnaDocument.Questions.Where(z => z.Contains(searchQuery)).Count() > 0);
            }

            return items;
        }

        /// <summary>
        /// this method can be used to publish the Kb.
        /// </summary>
        /// <param name="kbId">kb id.</param>
        /// <returns>task.</returns>
        public async Task PublishQnaAsync(string kbId)
        {
            await this.qnaMakerClient.Knowledgebase.PublishAsync(kbId);
        }

        /// <summary>
        /// this method can be used to get endpoint keys of qnamaker service.
        /// </summary>
        /// <returns>primary endpoint key.</returns>
        public async Task<string> GetQnaMakerEndpointKeysAsync()
        {
           var endpointKeys = await this.qnaMakerClient.EndpointKeys.GetKeysAsync();
           return endpointKeys?.PrimaryEndpointKey;
        }

        /// <summary>
        /// this method is used to create knowledgebase.
        /// </summary>
        /// <returns>kb id.</returns>
        public async Task<string> CreateKnowledgeBaseAsync()
        {
            var createOp = await this.qnaMakerClient.Knowledgebase.CreateAsync(new CreateKbDTO()
            {
                Name = Constants.KbName,
                QnaList = new List<QnADTO>()
                {
                    new QnADTO()
                    {
                        Answer = Constants.DummyAnswer,
                        Questions = new List<string>() { Constants.DummyQuestion },
                        Source = Constants.Source,
                        Metadata = new List<MetadataDTO>()
                        {
                            new MetadataDTO()
                            {
                                Name = Constants.MetadataTeamId,
                                Value = "dummy",
                            },
                        },
                    },
                },
            });

            createOp = await this.MonitorOperation(createOp);
            return createOp?.ResourceLocation?.Split('/').Last();
        }

        /// <summary>
        /// get kb details based on teamId.
        /// </summary>
        /// <param name="teamId">team id.</param>
        /// <returns>kb mapping.</returns>
        public async Task<TeamKbMapping> GetKbMappingAsync(string teamId)
        {
            var result = await this.storageProvider.ExecuteQueryUsingPointQueryAsync<TeamKbMapping>(Constants.MsTeams, teamId);
            return result;
        }

        /// <summary>
        /// gets all the KB mappings from storage.
        /// </summary>
        /// <returns>all kb mappings.</returns>
        public async Task<IList<TeamKbMapping>> GetAllKbMappingsAsync()
        {
            var response = await this.storageProvider.GetKbMappingsAsync();
            return response;
        }

        /// <summary>
        /// this method is used to create a teamId-KbId mapping entry in storage.
        /// </summary>
        /// <param name="teamId">team id.</param>
        /// <param name="knowledgeBaseId">kb id.</param>
        /// <param name="endpointKey">endpoint key.</param>
        /// <returns>boolean result.</returns>
        public async Task<bool> CreateKbMappingAsync(string teamId, string knowledgeBaseId, string endpointKey)
        {
            TeamKbMapping teamKbMapping = new TeamKbMapping(Constants.MsTeams, teamId)
            {
                KbId = knowledgeBaseId,
                EndpointKey = endpointKey,
            };

            var result = await this.storageProvider.InsertOrMergeTableEntityAsync<TeamKbMapping>(teamKbMapping);
            return (result != null) ? true : false;
        }

        /// <summary>
        /// this method can be used to monitor any qnamaker operation.
        /// </summary>
        /// <param name="operation">operation details.</param>
        /// <returns>operation.</returns>
        private async Task<Operation> MonitorOperation(Operation operation)
        {
            // Loop while operation is success
            for (int i = 0;
                i < 10 && (operation.OperationState == OperationStateType.NotStarted || operation.OperationState == OperationStateType.Running);
                i++)
            {
                await Task.Delay(5000);
                operation = await this.qnaMakerClient.Operations.GetDetailsAsync(operation.OperationId);
            }

            if (operation.OperationState != OperationStateType.Succeeded)
            {
                throw new Exception($"Operation {operation.OperationId} failed to completed.");
            }

            return operation;
        }
    }
}
