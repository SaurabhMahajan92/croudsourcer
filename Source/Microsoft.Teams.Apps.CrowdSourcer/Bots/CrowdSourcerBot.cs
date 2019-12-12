// <copyright file="CrowdSourcerBot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.Bots
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CrowdSourcer.Cards;
    using Microsoft.Teams.Apps.CrowdSourcer.Resources;
    using Microsoft.Teams.Apps.CrowdSourcer.Services;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// crowd sourcer bot.
    /// </summary>
    public class CrowdSourcerBot : TeamsActivityHandler
    {
        private TelemetryClient telemetryClient;
        private IQnaServiceProvider qnaServiceProvider;
        private IConfiguration configuration;
        private IStorageProvider storageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrowdSourcerBot"/> class.
        /// </summary>
        /// <param name="telemetryClient">telemetry client.</param>
        /// <param name="qnaServiceProvider">qnA maker service provider.</param>
        /// <param name="microsoftAppCredentials">app credentials.</param>
        /// <param name="configuration">configuration settings.</param>
        /// <param name="messagingExtensionQueryHandler">messaging extension.</param>
        /// <param name="storageProvider">storage provider.</param>
        public CrowdSourcerBot(TelemetryClient telemetryClient, IQnaServiceProvider qnaServiceProvider, IConfiguration configuration, IStorageProvider storageProvider)
        {
            this.telemetryClient = telemetryClient;
            this.qnaServiceProvider = qnaServiceProvider;
            this.configuration = configuration;
            this.storageProvider = storageProvider;
        }

        /// <inheritdoc/>
        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (!this.IsActivityFromExpectedTenant(turnContext))
                {
                    this.telemetryClient.TrackTrace($"Unexpected tenant id {turnContext.Activity.Conversation.TenantId}", SeverityLevel.Warning);
                    return Task.CompletedTask;
                }

                // Get the current culture info to use in resource files
                if (turnContext.Activity.Locale != null)
                {
                   CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CrowdSourcerResource.Culture = new CultureInfo(turnContext.Activity.Locale);
                }

                switch (turnContext.Activity.Type)
                {
                    case ActivityTypes.Message:
                        return this.OnMessageActivityAsync(new DelegatingTurnContext<IMessageActivity>(turnContext), cancellationToken);

                    default:
                        return base.OnTurnAsync(turnContext, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                return base.OnTurnAsync(turnContext, cancellationToken);
            }
        }

        /// <inheritdoc/>
        protected override async Task OnMessageActivityAsync(
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            try
            {
                var message = turnContext.Activity;

                await this.SendTypingIndicatorAsync(turnContext);

                if (message.Conversation.ConversationType.Equals("channel"))
                {
                    this.telemetryClient.TrackTrace($"Received message activity from: {message.From?.Id}, conversation: {message.Conversation.Id}, replyToId: {message.ReplyToId}");
                    await this.OnMessageActivityInChannelAsync(message, turnContext, cancellationToken);
                }
                else
                {
                    this.telemetryClient.TrackTrace($"Received unexpected conversationType {message.Conversation.ConversationType} from: {message.From?.Id}, conversation: {message.Conversation.Id}, replyToId: {message.ReplyToId}", SeverityLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error processing message: {ex.Message}", SeverityLevel.Error);
                this.telemetryClient.TrackException(ex);
            }
        }

        /// <summary>
        /// Handle member added activity in teams.
        /// </summary>
        /// <param name="membersAdded">member details.</param>
        /// <param name="turnContext">turn context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>task.</returns>
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var activity = turnContext.Activity;
                this.telemetryClient.TrackTrace($"conversationType: {activity.Conversation.ConversationType}, membersAdded: {activity.MembersAdded?.Count}, membersRemoved: {activity.MembersRemoved?.Count()}");
                bool isKbMappingCreated = default;
                if (activity.Conversation.ConversationType.Equals("channel"))
                {
                    if (membersAdded.Any(m => m.Id == activity.Recipient.Id))
                    {
                        // Bot was added to a team
                        this.telemetryClient.TrackTrace($"Bot added to team {activity.Conversation.Id}");

                        var teamDetails = ((JObject)turnContext.Activity.ChannelData).ToObject<TeamsChannelData>();

                        // get endpoint key
                        string endpointKey = await this.qnaServiceProvider.GetQnaMakerEndpointKeysAsync();

                        // check if kb mapping exists for any team.
                        var kbMappings = await this.qnaServiceProvider.GetAllKbMappingsAsync();
                        if (kbMappings == null || kbMappings?.Count == 0)
                        {
                            // only create kb if not exists for any team.
                            string kbId = await this.qnaServiceProvider.CreateKnowledgeBaseAsync();
                            if (!string.IsNullOrEmpty(kbId))
                            {
                                this.telemetryClient.TrackTrace($"kb created : {kbId}");
                                isKbMappingCreated = await this.qnaServiceProvider.CreateKbMappingAsync(teamDetails.Team.Id, kbId, endpointKey);
                                if (!isKbMappingCreated)
                                {
                                    this.telemetryClient.TrackTrace($"kb mapping not created team id: {teamDetails.Team.Id} kbid: {kbId}");
                                    await turnContext.SendActivityAsync(MessageFactory.Text(CrowdSourcerResource.ErrorMsgText));
                                }
                                else
                                {
                                    this.telemetryClient.TrackTrace($"kb mapping created team id: {teamDetails.Team.Id} kbid: {kbId}");
                                    await this.qnaServiceProvider.PublishQnaAsync(kbId);
                                }
                            }
                        }
                        else
                        {
                            // if kb mapping not exists => create mapping
                            if (!kbMappings.Any(m => m.RowKey.Contains(teamDetails.Team.Id) && !string.IsNullOrEmpty(m.KbId)))
                            {
                                isKbMappingCreated = await this.qnaServiceProvider.CreateKbMappingAsync(teamDetails.Team.Id, kbMappings?.FirstOrDefault().KbId, endpointKey);
                                if (!isKbMappingCreated)
                                {
                                    await turnContext.SendActivityAsync(MessageFactory.Text(CrowdSourcerResource.ErrorMsgText));
                                }
                                else
                                {
                                    this.telemetryClient.TrackTrace($"kb mapping created team id: {teamDetails.Team.Id} kbid: {kbMappings?.FirstOrDefault().KbId}");
                                }
                            }
                        }

                        await turnContext.SendActivityAsync(MessageFactory.Attachment(CrowdsourcerCards.WelcomeCard()));
                    }
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }
        }

        /// <summary>
        /// Handle message extension query received by the bot.
        /// </summary>
        /// <param name="turnContext">turn context.</param>
        /// <param name="query">query.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>MessagingExtensionResponse.</returns>
        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, Bot.Schema.Teams.MessagingExtensionQuery query, CancellationToken cancellationToken)
        {
            try
            {
                var messageExtensionQuery = JsonConvert.DeserializeObject<MessagingExtensionQuery>(turnContext.Activity.Value.ToString());
                var searchQuery = this.GetSearchQueryString(messageExtensionQuery);
                this.telemetryClient.TrackTrace($"searchQuery : {searchQuery} commandId : {messageExtensionQuery.CommandId}");
                turnContext.Activity.TryGetChannelData<TeamsChannelData>(out var teamsChannelData);

                return new MessagingExtensionResponse
                {
                    ComposeExtension = await this.GetSearchResultAsync(searchQuery, messageExtensionQuery.CommandId, teamsChannelData?.Team.Id),
                };
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult()
                {
                    Type = "message",
                    Text = CrowdSourcerResource.NoEntriesFound,
                },
            };
        }

        /// <summary>
        /// Handle message extension action fetch task received by the bot.
        /// </summary>
        /// <param name="turnContext">turn context.</param>
        /// <param name="action">action.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>MessagingExtensionActionResponse.</returns>
        protected override Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            try
            {
                var adaptiveCardEditor = CrowdsourcerCards.AddQuestionActionCard(isValid: true);

                return Task.FromResult(new MessagingExtensionActionResponse
                {
                    Task = new TaskModuleContinueResponse
                    {
                        Value = new TaskModuleTaskInfo
                        {
                            Card = adaptiveCardEditor,
                            Height = "medium",
                            Width = "medium",
                            Title = CrowdSourcerResource.TaskModuleTitle,
                        },
                    },
                });
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            return default;
        }

        /// <summary>
        /// Handle message extension submit action received by the bot.
        /// </summary>
        /// <param name="turnContext">turn context.</param>
        /// <param name="action">action.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>MessagingExtensionActionResponse.</returns>
        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            try
            {
                turnContext.Activity.TryGetChannelData<TeamsChannelData>(out var teamsChannelData);
                var submittedDetails = ((JObject)turnContext.Activity.Value).GetValue("data").ToObject<AdaptiveSubmitActionData>();
                string questionText = submittedDetails?.Question?.Trim();
                string answerText = submittedDetails?.Answer?.Trim();

                if (string.IsNullOrWhiteSpace(questionText))
                {
                    var adaptiveCardEditor = CrowdsourcerCards.AddQuestionActionCard(isValid: false);
                    return new MessagingExtensionActionResponse
                    {
                        Task = new TaskModuleContinueResponse
                        {
                            Value = new TaskModuleTaskInfo
                            {
                                Card = adaptiveCardEditor,
                                Height = "medium",
                                Width = "medium",
                                Title = CrowdSourcerResource.TaskModuleTitle,
                            },
                        },
                    };
                }

                if (string.IsNullOrWhiteSpace(answerText))
                {
                    answerText = Constants.Unanswered;
                }

                // setting conversation id as '#' when question is added from task module.
                await this.qnaServiceProvider.AddQnaAsync(questionText, answerText, turnContext.Activity.From.Name, teamsChannelData.Team.Id, "#").ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"Question : {questionText} AddedBy: {turnContext.Activity.Conversation.AadObjectId}  Answer: {answerText}");

                // send card.
                await turnContext.SendActivityAsync(MessageFactory.Attachment(CrowdsourcerCards.AddedAnswer(questionText, answerText, turnContext.Activity.From.Name, true)), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            return default;
        }

        /// <summary>
        /// Handle task module fetch activity from welcome card.
        /// </summary>
        /// <param name="turnContext">turn context.</param>
        /// <param name="taskModuleRequest">task module request.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>task module response.</returns>
        protected override Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            try
            {
                var adaptiveCardEditor = CrowdsourcerCards.AddQuestionActionCard(isValid: true);
                return Task.FromResult(new TaskModuleResponse
                {
                    Task = new TaskModuleContinueResponse
                    {
                        Value = new TaskModuleTaskInfo()
                        {
                            Card = adaptiveCardEditor,
                            Height = "medium",
                            Width = "medium",
                            Title = CrowdSourcerResource.TaskModuleTitle,
                        },
                    },
                });
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            return default;
        }

        /// <summary>
        /// Handle task module submit action.
        /// </summary>
        /// <param name="turnContext">turn context.</param>
        /// <param name="taskModuleRequest">task mmodule request.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>task module response.</returns>
        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            try
            {
                turnContext.Activity.TryGetChannelData<TeamsChannelData>(out var teamsChannelData);
                var submittedDetails = ((JObject)turnContext.Activity.Value).GetValue("data").ToObject<AdaptiveSubmitActionData>();
                string questionText = submittedDetails?.Question?.Trim();
                string answerText = submittedDetails?.Answer?.Trim();

                if (string.IsNullOrWhiteSpace(questionText))
                {
                    var adaptiveCardEditor = CrowdsourcerCards.AddQuestionActionCard(isValid: false);
                    return new TaskModuleResponse
                    {
                        Task = new TaskModuleContinueResponse
                        {
                            Value = new TaskModuleTaskInfo
                            {
                                Card = adaptiveCardEditor,
                                Height = "medium",
                                Width = "medium",
                                Title = CrowdSourcerResource.TaskModuleTitle,
                            },
                        },
                    };
                }

                if (string.IsNullOrWhiteSpace(answerText))
                {
                    answerText = Constants.Unanswered;
                }

                // setting conversation id as '#' when question is added from task module.
                await this.qnaServiceProvider.AddQnaAsync(questionText, answerText, turnContext.Activity.From.Name, teamsChannelData.Team.Id, "#").ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"Question : {questionText} AddedBy: {turnContext.Activity.Conversation.AadObjectId}  Answer: {answerText}");

                // send card.
                await turnContext.SendActivityAsync(MessageFactory.Attachment(CrowdsourcerCards.AddedAnswer(questionText, answerText, turnContext.Activity.From.Name, true)), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            return default;
        }

        /// <summary>
        /// Handle message activity in channel.
        /// </summary>
        /// <param name="message"> Message.</param>
        /// <param name="turnContext">Turn Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>A task.</returns>
        private async Task OnMessageActivityInChannelAsync(IMessageActivity message, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string text = turnContext.Activity.RemoveRecipientMention();

            QnASearchResultList qnaSearchResult = default;
            AdaptiveSubmitActionData activityValue = default;
            Attachment attachment = default;
            string answer = default;
            string updatedQuestion = default;

            turnContext.Activity.TryGetChannelData<TeamsChannelData>(out var teamsChannelData);
            var activity = (Activity)turnContext.Activity;

            switch (text.ToLower())
            {
                case Constants.SaveCommand:
                    activityValue = ((JObject)activity.Value).ToObject<AdaptiveSubmitActionData>();
                    answer = activityValue?.Answer?.Trim();
                    updatedQuestion = activityValue?.Question?.Trim();

                    if (string.IsNullOrWhiteSpace(updatedQuestion))
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text(CrowdSourcerResource.EmptyQnaValidation));
                        return;
                    }

                    // save if the qna is available in prod kb.
                    bool isSaved = await this.SaveActionAsync(turnContext, isTest: false, activityValue.Details?.Question, teamsChannelData?.Team.Id, updatedQuestion, answer);

                    if (!isSaved)
                    {
                        // save if the qna is available in test kb.
                        await this.SaveActionAsync(turnContext, isTest: true, activityValue.Details?.Question, teamsChannelData?.Team.Id, updatedQuestion, answer);
                    }

                    break;

                case Constants.DeleteCommand:
                    activityValue = ((JObject)activity.Value).ToObject<AdaptiveSubmitActionData>();
                    qnaSearchResult = await this.qnaServiceProvider.GenerateAnswerAsync(false, activityValue?.Details?.Question, teamsChannelData?.Team.Id);
                    if (qnaSearchResult.Answers.FirstOrDefault().Id != -1)
                    {
                            await this.qnaServiceProvider.DeleteQnaAsync(qnaSearchResult.Answers.FirstOrDefault().Id, teamsChannelData?.Team.Id);
                            this.telemetryClient.TrackTrace($"Question : {activityValue?.Details?.Question} DeletedBy: {activity.Conversation.AadObjectId}");
                            attachment = CrowdsourcerCards.DeletedEntry(activityValue?.Details?.Question, qnaSearchResult.Answers.FirstOrDefault().Answer, activity.From.Name);
                            var updateCardActivity = new Activity(ActivityTypes.Message)
                            {
                                Id = turnContext.Activity.ReplyToId,
                                Conversation = turnContext.Activity.Conversation,
                                Attachments = new List<Attachment> { attachment },
                            };

                            // send card.
                            await turnContext.UpdateActivityAsync(updateCardActivity, cancellationToken);
                            await turnContext.SendActivityAsync(MessageFactory.Text("**" + string.Format(CrowdSourcerResource.DeletedQnaPair, activity.From.Name) + ".**"), cancellationToken);
                    }
                    else
                    {
                        // check if qna is present in unpublished version.
                        qnaSearchResult = await this.qnaServiceProvider.GenerateAnswerAsync(true, activityValue?.Details?.Question, teamsChannelData?.Team.Id);

                        if (qnaSearchResult.Answers.FirstOrDefault().Id != -1)
                        {
                            await turnContext.SendActivityAsync(MessageFactory.Text(CrowdSourcerResource.WaitMessage));
                        }
                    }

                    break;

                case Constants.SubmitAddCommand:
                    activityValue = ((JObject)activity.Value).ToObject<AdaptiveSubmitActionData>();
                    answer = activityValue?.Answer?.Trim();
                    updatedQuestion = activityValue?.Question?.Trim();

                    if (string.IsNullOrWhiteSpace(updatedQuestion))
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text(CrowdSourcerResource.EmptyQnaValidation));
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(answer))
                    {
                        answer = Constants.Unanswered;
                    }

                    await this.qnaServiceProvider.AddQnaAsync(updatedQuestion, answer, activity.From.Name, teamsChannelData?.Team.Id, turnContext.Activity.Conversation.Id);
                    this.telemetryClient.TrackTrace($"Question : {updatedQuestion} UpdatedBy: {activity.Conversation.AadObjectId}  Answer: {answer}");
                    attachment = CrowdsourcerCards.AddedAnswer(updatedQuestion, answer, activity.From.Name, true);

                    var updateCard = new Activity(ActivityTypes.Message)
                    {
                        Id = turnContext.Activity.ReplyToId,
                        Conversation = turnContext.Activity.Conversation,
                        Attachments = new List<Attachment> { attachment },
                    };

                    // send card.
                    await turnContext.UpdateActivityAsync(updateCard, cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text(string.Format(CrowdSourcerResource.UpdatedQnaPair, activity.From.Name)), cancellationToken);
                    break;

                case Constants.AddCommand:
                    attachment = new Attachment()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = CrowdsourcerCards.AddQuestionAnswer(),
                    };
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment));
                    break;

                case Constants.NoCommand:
                    return;

                default:
                    this.telemetryClient.TrackTrace($"Sending input to QnAMaker: {text}");

                    // check if the answer in available in published KB.
                    qnaSearchResult = await this.qnaServiceProvider.GenerateAnswerAsync(isTest: false, text, teamsChannelData?.Team.Id);

                    if (qnaSearchResult?.Answers.FirstOrDefault().Id != -1)
                    {
                        var metadata = qnaSearchResult?.Answers?.FirstOrDefault().Metadata;

                        // metadata name value is stored in kb as lowercase. Hence need to convert it to pascal case.
                        string locale = string.IsNullOrEmpty(activity.Locale) ? Constants.DefaultLocale : activity.Locale;
                        TextInfo textInfo = new CultureInfo(locale, false).TextInfo;
                        string name = textInfo.ToTitleCase(metadata.Where(x => x.Name.Contains(Constants.MetadataCreatedBy)).SingleOrDefault().Value);
                        attachment = qnaSearchResult.Answers.FirstOrDefault().Answer.Equals(Constants.Unanswered)
                            ? CrowdsourcerCards.NoAnswerCard(text)
                            : CrowdsourcerCards.AddedAnswer(qnaSearchResult.Answers.FirstOrDefault().Questions.FirstOrDefault().ToString(), qnaSearchResult.Answers.FirstOrDefault().Answer, name, false);
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment));
                    }
                    else
                    {
                        // check if the answer in available in unpublished KB.
                        qnaSearchResult = await this.qnaServiceProvider.GenerateAnswerAsync(isTest: true, text, teamsChannelData?.Team.Id);

                        if (qnaSearchResult.Answers.FirstOrDefault().Id != -1)
                        {
                            if (qnaSearchResult.Answers.FirstOrDefault().Answer.Equals(Constants.Unanswered))
                            {
                                await turnContext.SendActivityAsync(MessageFactory.Attachment(CrowdsourcerCards.NoAnswerCard(qnaSearchResult.Answers.FirstOrDefault().Questions.FirstOrDefault().ToString())));
                            }
                            else
                            {
                                await turnContext.SendActivityAsync(MessageFactory.Text($"{CrowdSourcerResource.WaitMessageQuestion.Replace("{text}", text)}"));
                            }
                        }
                        else
                        {
                            // if question is not available in Kb, then add it with answer value as #$unanswered$#
                            await this.qnaServiceProvider.AddQnaAsync(text, Constants.Unanswered, turnContext.Activity.From.Name, teamsChannelData?.Team.Id, turnContext.Activity.Conversation.Id);
                            this.telemetryClient.TrackTrace($"Question : {text} QuestionedBy: {turnContext.Activity.Conversation.AadObjectId}");

                            await turnContext.SendActivityAsync(MessageFactory.Attachment(CrowdsourcerCards.NoAnswerCard(text)));
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// method perform update operation of qna pair.
        /// </summary>
        /// <param name="turnContext">turn context.</param>
        /// <param name="isTest">environment dev/prod.</param>
        /// <param name="question">question.</param>
        /// <param name="teamId">team id.</param>
        /// <param name="updatedQuestion">updated question.</param>
        /// <param name="answer">answer.</param>
        /// <returns>boolean result.</returns>
        private async Task<bool> SaveActionAsync(ITurnContext turnContext, bool isTest, string question, string teamId, string updatedQuestion, string answer)
        {
            var qnaSearchResult = await this.qnaServiceProvider.GenerateAnswerAsync(isTest, question, teamId);

            if (qnaSearchResult.Answers.FirstOrDefault().Id != -1)
            {
                await this.qnaServiceProvider.UpdateQnaAsync(qnaSearchResult.Answers.FirstOrDefault().Id, answer, turnContext.Activity.From.Name, updatedQuestion, question, teamId);
                this.telemetryClient.TrackTrace($"Question : {question} UpdatedBy: {turnContext.Activity.Conversation.AadObjectId}  Answer: {answer}");
                var attachment = CrowdsourcerCards.AddedAnswer(updatedQuestion, answer, turnContext.Activity.From.Name, true);

                var updateCardActivity = new Activity(ActivityTypes.Message)
                {
                    Id = turnContext.Activity.ReplyToId,
                    Conversation = turnContext.Activity.Conversation,
                    Attachments = new List<Attachment> { attachment },
                };

                // send card.
                await turnContext.UpdateActivityAsync(updateCardActivity, cancellationToken: default);
                await turnContext.SendActivityAsync(MessageFactory.Text(string.Format(CrowdSourcerResource.UpdatedQnaPair, turnContext.Activity.From.Name)), cancellationToken: default);
            }
            else
            {
                if (isTest)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(CrowdSourcerResource.QuestionNotAvailable));
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Send typing indicator to the user.
        /// </summary>
        /// <param name="turnContext">ITurnContext object.</param>
        /// <returns>A task.</returns>
        private Task SendTypingIndicatorAsync(ITurnContext turnContext)
        {
            var typingActivity = turnContext.Activity.CreateReply();
            typingActivity.Type = ActivityTypes.Typing;
            return turnContext.SendActivityAsync(typingActivity);
        }

        /// <summary>
        /// Verify if the tenant Id in the message is the same tenant Id used when application was configured.
        /// </summary>
        /// <param name="turnContext">turn context.</param>
        /// <returns>boolean for expected tenant id.</returns>
        private bool IsActivityFromExpectedTenant(ITurnContext turnContext)
        {
            return turnContext.Activity.Conversation.TenantId == this.configuration["TenantId"];
        }

        /// <summary>
        /// Get search result.
        /// </summary>
        /// <param name="searchQuery">searchQuery.</param>
        /// <param name="commandId">commandId.</param>
        /// <param name="teamId">teamId.</param>
        /// <returns>Compose extension result.</returns>
        private async Task<MessagingExtensionResult> GetSearchResultAsync(string searchQuery, string commandId, string teamId)
        {
            MessagingExtensionResult composeExtensionResult = new MessagingExtensionResult()
            {
                Type = "result",
                AttachmentLayout = AttachmentLayoutTypes.List,
                Attachments = new List<MessagingExtensionAttachment>(),
            };

            IEnumerable<QnADTO> qnaDocuments = default;

            switch (commandId)
            {
                case Constants.CreatedCommandId:
                    qnaDocuments = await this.qnaServiceProvider.DownloadKnowledgebaseAsync(teamId, true, searchQuery);
                    var ordercreatedFiltered = qnaDocuments.Select(p => new { p.Id, p.Questions, p.Source, p.Answer, p.Metadata.Where(x => x.Name == Constants.MetadataCreatedAt)?.FirstOrDefault()?.Value, p.Metadata }).OrderByDescending(w => w.Value);
                    qnaDocuments = ordercreatedFiltered.Select(p => new QnADTO { Id = p.Id, Questions = p.Questions, Source = p.Source, Answer = p.Answer, Metadata = p.Metadata });
                    break;

                case Constants.EditedCommandId:
                    qnaDocuments = await this.qnaServiceProvider.DownloadKnowledgebaseAsync(teamId, true, searchQuery);
                    var ordereditedFiltered = qnaDocuments.Select(p => new { p.Id, p.Questions, p.Source, p.Answer, p.Metadata.Where(x => x.Name == Constants.MetadataUpdatedAt)?.FirstOrDefault()?.Value, p.Metadata }).OrderByDescending(w => w.Value);
                    qnaDocuments = ordereditedFiltered.Select(p => new QnADTO { Id = p.Id, Questions = p.Questions, Source = p.Source, Answer = p.Answer, Metadata = p.Metadata });
                    break;

                case Constants.UnAnsweredCommandId:
                    qnaDocuments = await this.qnaServiceProvider.DownloadKnowledgebaseAsync(teamId, false, searchQuery);
                    var orderunansweredFiltered = qnaDocuments.Select(p => new { p.Id, p.Questions, p.Source, p.Answer, p.Metadata.Where(x => x.Name == Constants.MetadataCreatedAt)?.FirstOrDefault()?.Value, p.Metadata }).OrderByDescending(w => w.Value);
                    qnaDocuments = orderunansweredFiltered.Select(p => new QnADTO { Id = p.Id, Questions = p.Questions, Source = p.Source, Answer = p.Answer, Metadata = p.Metadata });
                    break;
            }

            composeExtensionResult.Attachments = CrowdsourcerCards.MessagingExtensionCardList(qnaDocuments);
            return composeExtensionResult;
        }

        /// <summary>
        /// Get the value of the searchText parameter in the ME query.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <returns>Message Extension Input Text.</returns>
        private string GetSearchQueryString(MessagingExtensionQuery query)
        {
            string messageExtensionInputText = string.Empty;
            foreach (var parameter in query.Parameters)
            {
                if (parameter.Name.Equals(Constants.CreatedCommandId, StringComparison.OrdinalIgnoreCase) || parameter.Name.Equals(Constants.EditedCommandId, StringComparison.OrdinalIgnoreCase) || parameter.Name.Equals(Constants.UnAnsweredCommandId, StringComparison.OrdinalIgnoreCase))
                {
                    messageExtensionInputText = parameter.Value.ToString();
                    break;
                }
            }

            return messageExtensionInputText;
        }
    }
}
