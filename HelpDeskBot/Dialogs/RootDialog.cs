using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using HelpDeskBot.Util;
using System.Collections.Generic;
using AdaptiveCards;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

namespace HelpDeskBot.Dialogs
{
    [Serializable]
    [LuisModel("dd4e5c67-4f11-4349-957b-dce7ef6d4f59", "fa9bf847b2ed4d34bd7ac2d33580c747")]
    //[LuisModel("{LUISAppID}", "{LUISKey}")] Replace the {LUISAppID} with the App ID you have saved from the LUIS Portal and the {LUISKey} with the Programmatic API Key you have saved from My Keys section.
    //public class RootDialog : IDialog<object>
    public class RootDialog : LuisDialog<object>
    {
        private string category;
        private string severity;
        private string description;

        public Task StartAsync(IDialogContext context)
        {
            //context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;

            //var severities = new string[] { "high", "medium", "low" };
            //PromptDialog.Choice(context, this.MessageReceivedAsync, severities, "Which severity do you want");
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"I'm sorry, I did not understand {result.Query}.\nType 'help' to know more about me :)");
            context.Done<object>(null);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I'm the help desk bot and I can help you create a ticket or explore the knowledge base.\n" +
                                    "You can tell me things like _I need to reset my password_ or _explore hardware articles_.");
            context.Done<object>(null);
        }

        [LuisIntent("SubmitTicket")]
        public async Task SubmitTicket(IDialogContext context, LuisResult result)
        {
            EntityRecommendation categoryEntityRecommendation, severityEntityRecommendation;

            result.TryFindEntity("category", out categoryEntityRecommendation);
            result.TryFindEntity("severity", out severityEntityRecommendation);

            this.category = ((Newtonsoft.Json.Linq.JArray)categoryEntityRecommendation?.Resolution["values"])?[0]?.ToString();
            this.severity = ((Newtonsoft.Json.Linq.JArray)severityEntityRecommendation?.Resolution["values"])?[0]?.ToString();
            this.description = result.Query;

            await this.EnsureTicket(context);
        }

        [LuisIntent("ExploreKnowledgeBase")]
        public async Task ExploreCategory(IDialogContext context, LuisResult result)
        {
            EntityRecommendation categoryEntityRecommendation;
            result.TryFindEntity("category", out categoryEntityRecommendation);
            var category = ((Newtonsoft.Json.Linq.JArray)categoryEntityRecommendation?.Resolution["values"])?[0]?.ToString();

            context.Call(new CategoryExplorerDialog(category, result.Query), this.ResumeAndEndDialogAsync);
        }

        private async Task ResumeAndEndDialogAsync(IDialogContext context, IAwaitable<object> argument)
        {
            context.Done<object>(null);
        }

        /* Confirm the ticket is complete */
        private async Task EnsureTicket(IDialogContext context)
        {
            if (this.severity == null)
            {
                var severities = new string[] { "high", "normal", "low" };
                PromptDialog.Choice(context, this.SeverityMessageReceivedAsync, severities, "Which is the severity of this problem?");
            }
            else if (this.category == null)
            {
                PromptDialog.Text(context, this.CategoryMessageReceivedAsync, "Which would be the category for this ticket (software, hardware, networking, security or other)?");
            }
            else
            {
                var text = $"Great! I'm going to create a **{this.severity}** severity ticket in the **{this.category}** category. " +
                        $"The description I will use is _\"{this.description}\"_. Can you please confirm that this information is correct?";

                PromptDialog.Confirm(context, this.IssueConfirmedMessageReceivedAsync, text);
            }
        }

        /* These grab the missing entities, and add them back in then go back to Ensure Ticket and try again. */
        private async Task SeverityMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            this.severity = await argument;
            await this.EnsureTicket(context);
        }
        private async Task CategoryMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            this.category = await argument;
            await this.EnsureTicket(context);
        }

        //private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        //{
        //    var activity = await result as Activity;

        //    // calculate something for us to return
        //    int length = (activity.Text ?? string.Empty).Length;

        //    // return our reply to the user
        //    await context.PostAsync($"You sent {activity.Text} which was {length} characters");

        //    context.Wait(MessageReceivedAsync);
        //}

        //private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        //{
        //    var severity = await argument;
        //    Console.WriteLine(severity);
        //}




        //public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        //{
        //    var message = await argument;
        //    await context.PostAsync("Hi! I’m the help desk bot and I can help you create a ticket.");
        //    PromptDialog.Text(context, this.DescriptionMessageReceivedAsync, "First, please briefly describe your problem to me.");
        //}

        //public async Task DescriptionMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        //{
        //    this.description = await argument;
        //    var severities = new string[] { "high", "normal", "low" };
        //    PromptDialog.Choice(context, this.SeverityMessageReceivedAsync, severities, "Which is the severity of this problem?");
        //}

        //public async Task SeverityMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        //{
        //    this.severity = await argument;
        //    PromptDialog.Text(context, this.CategoryMessageReceivedAsync, "Which would be the category for this ticket (software, hardware, networking, security or other)?");
        //}

        //public async Task CategoryMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        //{
        //    this.category = await argument;
        //    var text = $"Great! I'm going to create a \"{this.severity}\" severity ticket in the \"{this.category}\" category. " +
        //                $"The description I will use is \"{this.description}\". Can you please confirm that this information is correct?";

        //    PromptDialog.Confirm(context, this.IssueConfirmedMessageReceivedAsync, text);
        //}



        public async Task IssueConfirmedMessageReceivedAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirmed = await argument;
                
            if (confirmed)
            {
                //CreateReply(String.Empty);

                //var reply = activity.CreateReply(String.Empty);
                //reply.Type = ActivityTypes.Typing;
                //await activityContext.SendResponse(reply);

                //var activity = await argument as Activity;

                //ConnectorClient connector = new ConnectorClient(new System.Uri(activity.ServiceUrl));
                //Activity isTypingReply = activity.CreateReply(“Shuttlebot is typing…”);
                //isTypingReply.Type = ActivityTypes.Typing;
                //isTypingReply.Text = “Shuttlebot is typing…”;
                //await connector.Conversations.ReplyToActivityAsync(isTypingReply);

                /* Send an 'is typing' message */
                var a = context.MakeMessage();
                a.Type = ActivityTypes.Typing;
                await context.PostAsync(a);
                await Task.Delay(2000);

                var api = new TicketAPIClient();
                var ticketId = await api.PostTicketAsync(this.category, this.severity, this.description);

                if (ticketId != -1)
                {
                    var message = context.MakeMessage();
                    message.Attachments = new List<Attachment>
            {
                new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = CreateCard(ticketId, this.category, this.severity, this.description)
                }
            };
                    await context.PostAsync(message);
                }
                else
                {
                    await context.PostAsync("Ooops! Something went wrong while I was saving your ticket. Please try again later.");
                }
            }
            else
            {
                await context.PostAsync("Ok. The ticket was not created. You can start again if you want.");
            }
            context.Done<object>(null);
        }


        private AdaptiveCard CreateCard(int ticketId, string category, string severity, string description)
        {
            AdaptiveCard card = new AdaptiveCard();

            var headerBlock = new TextBlock()
            {
                Text = $"Ticket #{ticketId}",
                Weight = TextWeight.Bolder,
                Size = TextSize.Large,
                Speak = $"<s>You've created a new Ticket #{ticketId}</s><s>We will contact you soon.</s>"
            };

            var columnsBlock = new ColumnSet()
            {
                Separation = SeparationStyle.Strong,
                Columns = new List<Column>
        {
            new Column
            {
                Size = "1",
                Items = new List<CardElement>
                {
                    new FactSet
                    {
                        Facts = new List<AdaptiveCards.Fact>
                        {
                            new AdaptiveCards.Fact("Severity:", severity),
                            new AdaptiveCards.Fact("Category:", category),
                        }
                    }
                }
            },
            new Column
            {
                Size = "auto",
                Items = new List<CardElement>
                {
                    new Image
                    {
                        Url = "https://raw.githubusercontent.com/GeekTrainer/help-desk-bot-lab/master/assets/botimages/head-smiling-medium.png",
                        Size = ImageSize.Small,
                        HorizontalAlignment = HorizontalAlignment.Right
                    }
                }
            }
        }
            };

            var descriptionBlock = new TextBlock
            {
                Text = description,
                Wrap = true
            };

            card.Body.Add(headerBlock);
            card.Body.Add(columnsBlock);
            card.Body.Add(descriptionBlock);

            return card;
        }

    }
}