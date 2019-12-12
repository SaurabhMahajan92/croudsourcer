// <copyright file="CrowdsourcerCards.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CrowdSourcer.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using AdaptiveCards;
    using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Teams.Apps.CrowdSourcer.Resources;

    /// <summary>
    /// create crowd sourcer cards.
    /// </summary>
    public class CrowdsourcerCards
    {
        /// <summary>
        /// returns the messaging extension attachment of all answers.
        /// </summary>
        /// <param name="qnaDocuments">all qnaDocuments.</param>
        /// <returns>returns the list of all answered questions.</returns>
        public static List<MessagingExtensionAttachment> MessagingExtensionCardList(IEnumerable<QnADTO> qnaDocuments)
        {
            var messagingExtensionAttachments = new List<MessagingExtensionAttachment>();

            foreach (var qnaDoc in qnaDocuments)
            {
                DateTime createdAt = (qnaDoc?.Metadata.Count > 1) ? new DateTime(long.Parse(qnaDoc?.Metadata?.Where(s => s.Name == Constants.MetadataCreatedAt).FirstOrDefault().Value)) : default;
                string dateString = default;
                string createdBy = default;
                string conversationId = default;

                if (qnaDoc?.Metadata?.Count > 1)
                {
                    string name = qnaDoc.Metadata.Where(s => s.Name == Constants.MetadataCreatedBy).FirstOrDefault().Value;
                    TextInfo textInfo = new CultureInfo(Constants.DefaultLocale, false).TextInfo;
                    createdBy = textInfo.ToTitleCase(name);
                    conversationId = qnaDoc.Metadata.Where(s => s.Name == Constants.MetadataConversationId).FirstOrDefault().Value;
                    dateString = "{{DATE(" + createdAt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'") + ", SHORT)}} at {{TIME(" + createdAt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") + ")}}";
                }

                string ans = qnaDoc.Answer.Equals(Constants.Unanswered) ? string.Empty : qnaDoc.Answer;

                var card = new AdaptiveCard("1.0")
                {
                    Body = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = $"**{CrowdSourcerResource.QuestionTitle}**: {qnaDoc.Questions[0]}",
                            Size = AdaptiveTextSize.Default,
                            Wrap = true,
                        },
                        new AdaptiveTextBlock
                        {
                            Text = string.IsNullOrEmpty(ans) ? string.Empty : $"**{CrowdSourcerResource.AnswerTitle}**: {ans}",
                            Size = AdaptiveTextSize.Default,
                            Wrap = true,
                        },
                        new AdaptiveTextBlock
                        {
                            Text = $"{createdBy} | {dateString}",
                            Wrap = true,
                        },
                    },
                };

                if (!conversationId.Equals("#"))
                {
                    string[] threadAndMessageId = ("19:" + conversationId).Split(";");
                    var threadId = threadAndMessageId[0];
                    var messageId = threadAndMessageId[1].Split("=")[1];

                    card.Actions.Add(
                        new AdaptiveOpenUrlAction()
                        {
                            Title = CrowdSourcerResource.GoToThread,
                            Url = new Uri($"https://teams.microsoft.com/l/message/{threadId}/{messageId}"),
                        });
                }

                string truncatedAnswer = ans.Length <= 50 ? ans : ans.Substring(0, 45) + "...";

                ThumbnailCard previewCard = new ThumbnailCard
                {
                    Title = $"<b>{qnaDoc.Questions[0]}</b>",
                    Text = $"{truncatedAnswer} <br/>{createdBy} | {createdAt} <br/>",
                };

                messagingExtensionAttachments.Add(new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card,
                }.ToMessagingExtensionAttachment(previewCard.ToAttachment()));
            }

            return messagingExtensionAttachments;
        }

        /// <summary>
        /// no answer found card.
        /// </summary>
        /// <param name="question">question.</param>
        /// <returns>attachment.</returns>
        public static Attachment NoAnswerCard(string question)
        {
            AdaptiveCard card = new AdaptiveCard("1.0");
            var container = new AdaptiveContainer()
            {
                Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Size = AdaptiveTextSize.Default,
                            Wrap = true,
                            Text = CrowdSourcerResource.AnswerNotFound,
                        },
                    },
            };
            card.Body.Add(container);
            card.Actions.Add(
                new AdaptiveShowCardAction()
                {
                    Title = CrowdSourcerResource.AddEntryTitle,
                    Card = UpdateEntry(question),
                });
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };

            return adaptiveCardAttachment;
        }

        /// <summary>
        /// welcome card.
        /// </summary>
        /// <returns>attachment.</returns>
        public static Attachment WelcomeCard()
        {
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = CrowdSourcerResource.WelcomeMessage,
                            Size = AdaptiveTextSize.Default,
                            Wrap = true,
                        },
                    },
            };

            card.Actions.Add(
               new AdaptiveSubmitAction()
               {
                   Title = CrowdSourcerResource.AskQuestion,
                   Data = new AdaptiveSubmitActionData
                   {
                       MsTeams = new CardAction
                       {
                           Type = "task/fetch",
                       },
                   },
               });

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };
        }

        /// <summary>
        /// updated answer card.
        /// </summary>
        /// <param name="question">question.</param>
        /// <param name="answer">answer.</param>
        /// <param name="editedBy">editedby.</param>
        /// <param name="isTest">boolean environment.</param>
        /// <returns>attachment.</returns>
        public static Attachment AddedAnswer(string question, string answer, string editedBy, bool isTest)
        {
            if (!string.IsNullOrWhiteSpace(answer))
            {
                answer = answer.Equals(Constants.Unanswered) ? string.Empty : answer;
            }

            AdaptiveCard card = new AdaptiveCard("1.0");
            var container = new AdaptiveContainer()
            {
                Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Size = AdaptiveTextSize.Default,
                            Wrap = true,
                            Text = $"**{CrowdSourcerResource.QuestionTitle}:** {question}",
                        },
                        new AdaptiveTextBlock
                        {
                            Size = AdaptiveTextSize.Default,
                            Wrap = true,
                            Text = string.IsNullOrWhiteSpace(answer) ? answer : $"**{CrowdSourcerResource.AnswerTitle}:** {answer}",
                        },
                        new AdaptiveTextBlock
                        {
                            Size = AdaptiveTextSize.Small,
                            Wrap = true,
                            Text = string.Format(CrowdSourcerResource.LastEdited, editedBy),
                        },
                    },
            };

            if (isTest)
            {
                container.Items.Add(new AdaptiveTextBlock
                {
                    Size = AdaptiveTextSize.Small,
                    Wrap = true,
                    Text = CrowdSourcerResource.WaitMessageAnswer,
                });
            }

            card.Body.Add(container);

            card.Actions.Add(
                new AdaptiveShowCardAction()
                {
                    Title = CrowdSourcerResource.UpdateEntryTitle,
                    Card = UpdateEntry(question, answer),
                });

            card.Actions.Add(
                new AdaptiveShowCardAction()
                {
                    Title = CrowdSourcerResource.DeleteEntryTitle,
                    Card = DeleteEntry(question),
                });

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };

            return adaptiveCardAttachment;
        }

        /// <summary>
        /// update toggle card.
        /// </summary>
        /// <param name="question">question.</param>
        /// <returns>card.</returns>
        public static AdaptiveCard AddQuestionAnswer()
        {
            AdaptiveCard card = new AdaptiveCard("1.0");
            var container = new AdaptiveContainer()
            {
                Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = CrowdSourcerResource.QuestionTitle,
                            Size = AdaptiveTextSize.Small,
                        },
                        new AdaptiveTextInput
                        {
                            Id = "question",
                            Placeholder = CrowdSourcerResource.PlaceholderQuestion,
                            MaxLength = 100,
                            Style = AdaptiveTextInputStyle.Text,
                        },
                        new AdaptiveTextBlock
                        {
                            Text = CrowdSourcerResource.AnswerTitle,
                            Size = AdaptiveTextSize.Small,
                        },
                        new AdaptiveTextInput
                        {
                            Id = "answer",
                            Placeholder = CrowdSourcerResource.PlaceholderAnswer,
                            IsMultiline = true,
                            MaxLength = 500,
                            Style = AdaptiveTextInputStyle.Text,
                        },
                    },
            };
            card.Body.Add(container);

            card.Actions.Add(
               new AdaptiveSubmitAction()
               {
                   Title = CrowdSourcerResource.Save,
                   Data = new AdaptiveSubmitActionData
                   {
                       MsTeams = new CardAction
                       {
                           Type = ActionTypes.MessageBack,
                           Text = Constants.SubmitAddCommand,
                           Value = "add",
                       },
                   },
               });

            return card;
        }

        /// <summary>
        /// update toggle card.
        /// </summary>
        /// <param name="question">question.</param>
        /// <param name="answer">answer.</param>
        /// <returns>card.</returns>
        public static AdaptiveCard UpdateEntry(string question, string answer = "")
        {
            AdaptiveCard card = new AdaptiveCard("1.0");
            var container = new AdaptiveContainer()
            {
                Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = CrowdSourcerResource.QuestionTitle,
                            Size = AdaptiveTextSize.Small,
                        },
                        new AdaptiveTextInput
                        {
                            Id = "question",
                            MaxLength = 100,
                            Placeholder = CrowdSourcerResource.PlaceholderQuestion,
                            Style = AdaptiveTextInputStyle.Text,
                            Value = question,
                        },
                        new AdaptiveTextBlock
                        {
                            Text = CrowdSourcerResource.AnswerTitle,
                            Size = AdaptiveTextSize.Small,
                        },
                        new AdaptiveTextInput
                        {
                            Id = "answer",
                            Placeholder = CrowdSourcerResource.PlaceholderAnswer,
                            IsMultiline = true,
                            MaxLength = 500,
                            Style = AdaptiveTextInputStyle.Text,
                            Value = answer,
                        },
                    },
            };
            card.Body.Add(container);

            card.Actions.Add(
               new AdaptiveSubmitAction()
               {
                   Title = CrowdSourcerResource.Save,
                   Data = new AdaptiveSubmitActionData
                   {
                       MsTeams = new CardAction
                       {
                           Type = ActionTypes.MessageBack,
                           Text = Constants.SaveCommand,
                           Value = "save",
                       },
                       Details = new Details() { Question = question },
                   },
               });

            return card;
        }

        /// <summary>
        /// delete toggle card.
        /// </summary>
        /// <param name="question">question.</param>
        /// <returns>card.</returns>
        public static AdaptiveCard DeleteEntry(string question)
        {
            AdaptiveCard card = new AdaptiveCard("1.0");
            var container = new AdaptiveContainer()
            {
                Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                           Text = CrowdSourcerResource.DeleteConfirmation,
                           Wrap = true,
                        },
                    },
            };
            card.Body.Add(container);

            card.Actions.Add(
               new AdaptiveSubmitAction()
               {
                   Title = CrowdSourcerResource.Yes,
                   Data = new AdaptiveSubmitActionData
                   {
                       MsTeams = new CardAction
                       {
                           Type = ActionTypes.MessageBack,
                           Text = Constants.DeleteCommand,
                           Value = "delete",
                       },
                       Details = new Details() { Question = question },
                   },
               });

            card.Actions.Add(
              new AdaptiveSubmitAction()
              {
                  Title = CrowdSourcerResource.No,
                  Data = new AdaptiveSubmitActionData
                  {
                      MsTeams = new CardAction
                      {
                          Type = ActionTypes.MessageBack,
                          Text = Constants.NoCommand,
                          Value = "no",
                      },
                  },
              });

            return card;
        }

        /// <summary>
        /// deleted item card.
        /// </summary>
        /// <param name="question">question.</param>
        /// <param name="answer">answer.</param>
        /// <param name="deletedBy">deleted by user.</param>
        /// <returns>card.</returns>
        public static Attachment DeletedEntry(string question, string answer, string deletedBy)
        {
            AdaptiveCard card = new AdaptiveCard("1.0");
            var container = new AdaptiveContainer()
            {
                Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Size = AdaptiveTextSize.Default,
                            Wrap = true,
                            Text = $"**{CrowdSourcerResource.QuestionTitle}:** {question}",
                        },
                        new AdaptiveTextBlock
                        {
                            Size = AdaptiveTextSize.Default,
                            Wrap = true,
                            Text = $"**{CrowdSourcerResource.AnswerTitle}:** {answer}",
                        },
                        new AdaptiveTextBlock
                        {
                            Size = AdaptiveTextSize.Small,
                            Wrap = true,
                            Text = string.Format(CrowdSourcerResource.DeletedQnaPair, deletedBy),
                        },
                        new AdaptiveTextBlock
                        {
                            Size = AdaptiveTextSize.Small,
                            Wrap = true,
                            Text = CrowdSourcerResource.WaitMessageAnswer,
                        },
                    },
            };
            card.Body.Add(container);

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };

            return adaptiveCardAttachment;
        }

        /// <summary>
        /// Add question card task module.
        /// </summary>
        /// <param name="isValid">validation flag.</param>
        /// <returns>card.</returns>
        public static Attachment AddQuestionActionCard(bool isValid)
        {
            AdaptiveCard card = new AdaptiveCard("1.0");
            var container = new AdaptiveContainer()
            {
                Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = CrowdSourcerResource.QuestionTitle,
                            Size = AdaptiveTextSize.Small,
                        },
                        new AdaptiveTextInput
                        {
                            Id = "question",
                            Placeholder = CrowdSourcerResource.PlaceholderQuestion,
                            MaxLength = 100,
                            Style = AdaptiveTextInputStyle.Text,
                        },
                        new AdaptiveTextBlock
                        {
                            Text = CrowdSourcerResource.AnswerTitle,
                            Size = AdaptiveTextSize.Small,
                        },
                        new AdaptiveTextInput
                        {
                            Id = "answer",
                            Placeholder = CrowdSourcerResource.PlaceholderAnswer,
                            IsMultiline = true,
                            MaxLength = 500,
                            Style = AdaptiveTextInputStyle.Text,
                        },
                    },
            };

            if (!isValid)
            {
                container.Items.Add(new AdaptiveTextBlock
                {
                    Text = CrowdSourcerResource.EmptyQnaValidation,
                    Size = AdaptiveTextSize.Small,
                    Color = AdaptiveTextColor.Attention,
                });
            }

            card.Body.Add(container);

            card.Actions.Add(
                new AdaptiveSubmitAction()
                {
                    Title = CrowdSourcerResource.SubmitTitle,
                    Data = new AdaptiveSubmitActionData
                    {
                        MsTeams = new CardAction
                        {
                            Type = "task/submit",
                        },
                    },
                });

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };

            return adaptiveCardAttachment;
        }
    }
}
