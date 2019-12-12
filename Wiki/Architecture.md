  

![Architecture Image](/wiki/images/architecture_overview.png)
 

The **Crowd Sourcer** application has the following main components:

  

- **QnAMaker**: Resources that comprise the QnAMaker cognitive service, which implements the "FAQ" part of the application. The installer creates a [knowledge base](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/knowledge-base) using the [tools](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/development-lifecycle-knowledge-base) provided by QnA Maker.

- **Azure Function**: Azure function is time triggered and will be invoked after every 15 minutes to read the Knowledge base and publish the KB. 

- **Crowd Sourcer Bot**: Crowdsourcer is a BOT that helps a group of people (team) collaborate to obtain voluntary answers to their queries in a fun and transparent manner. It works on the principle of tapping on to the crowd intelligence and collective wisdom of the group.



- The knowledge base (KB) in QnA Maker is presented in a team as  conversational bot interface. Through the bot, the user can ask questions, add, update, delete & view questions.

- The same bot also implements a messaging extension that have the following tabs
	-	Recently Created: Shows the list of the most recently submitted questions with the newest of the questions appearing first in the list
   - Recently Edited: Shows the list of the most recently edited questions 
   -	Unanswered: Shows the list of Unanswered questions with the newest of the questions appearing first in the list

**QnA Maker**

 

Crowd Sourcer uses QnA Maker to query the KB for questions and answers. As soon as the Bot is successfully installed, a knowledge base will be created if its first instance of bot getting installed on tenant. A tenant will use the same KB across multiple teams. 
The precision and recall of the bot responses to user questions are directly tied to the quality of the knowledge base, so it's important to follow QnA Maker's recommended [best practices](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/best-practices). Keep in mind that a good knowledge base requires curation and feedback: see [Development lifecycle of a knowledge base](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/development-lifecycle-knowledge-base).

  

For more details about QnAMaker, please refer to the [QnAMaker documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/overview/overview).

  

**Bot and Messaging Extension**

  

The bot is built using the [Bot Framework SDK v4 for .NET](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-overview-introduction?view=azure-bot-service-4.0) and [ASP.NET Core 2.](https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-2.0)1. The bot has a conversational interface with team  scope. It also implements a messaging extension with [query commands](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/messaging-extensions/search-extensions), which the team users can use to see the list of questions from different category.

## Bot Commands

![Bot Installation](/wiki/Images/Bot_Installation.PNG)

-  **Take a tour:** Carousel card is shown with images and description of bot commands. The content of cards i.e. illustrations and text are provided in resource file. 
-  **Ask question:** Bot consider any question after @mention bot as a question and search in QnA maker to find the answer. 
![Bot Installation](/wiki/Images/Ask_Question.PNG)
-  **Add question:** Bot has reserved commands for ‘Add Question’ and ‘Take a tour’ to respond back with cards. Add question can be invoked as bot command and also from Messaging extension. 

![Bot Installation](/wiki/Images/Add_Question.PNG)
