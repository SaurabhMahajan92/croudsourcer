
- **Knowledge base quality:** A good bot experience for your team users is critically depended upon the quality of your knowledge base. Please refer to this links on helpful best practices on how to set up useful knowledge bases.

	- [https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/best-practices](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/best-practices)

	- [https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/cognitive-services/QnAMaker/Concepts/best-practices.md](https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/cognitive-services/QnAMaker/Concepts/best-practices.md)

	Tip: If you don't have the complete knowledge base ready, here is a [Teams Frequently Asked Questions Link](https://support.office.com/en-us/article/faq-f4644010-d5fa-4055-b42a-6a5317316e18) that works well with QnA Maker.

- **Supported data sources in QnA Maker:** QnA maker is pretty robust in supporting different data source types. Please refer to this [link](https://docs.microsoft.com/en-us/azure/cognitive-services/QnAMaker/concepts/data-sources-supported) on supported data sources and how to get started with each.

- **Allow non-developers to edit Knowledge Base:** Instructions here to grant Owner/Contributor to the Cognitive service and allow non dev to edit KB from QnAMaker portal. Please refer to the [link](https://docs.microsoft.com/en-us/azure/cognitive-services/QnAMaker/how-to/collaborate-knowledge-base) to grant access.

- **Managing user expectations:**

	- Each time a team member asks a question to the bot, the question gets added in the KB as an Unanswered Question.
	
	- All team members will be able to update or delete a QnA pair.

	- All crud operations on the KB are queued and all the pending edits are then published to production KB at regular intervals.

