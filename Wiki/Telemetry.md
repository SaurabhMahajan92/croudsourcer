# Telemetry

The Teams Bot app logs telemetry to  [Azure Application Insights](https://azure.microsoft.com/en-us/services/monitor/). You can go to the respective Application Insights blade of the Azure App Services to view basic telemetry about your services, such as requests, failures, and dependency errors, custom events, traces etc. .

The Teams Bot integrates with Application Insights to gather bot activity analytics, as described  [here](https://blog.botframework.com/2019/03/21/bot-analytics-behind-the-scenes/).

The Teams Bot logs a few kinds of events:

The  `Activity`  event:

-   Basic activity info:  `ActivityId`,  `ActivityType`,  `Event Name`
-   Basic user info:  `From ID`

The  `UserActivity`  event:

-   Basic activity info:  `ActivityId`,  `ActivityType`,  `Event Name`
-   Basic user info:  `UserAadObjectId`
-   Context of how it was invoked:  `ConversationType`,  `TeamId`

The  `ProcessedPairups`  event:

-   Basic activity info:  `PairsNotifiedCount`,  `UsersNotifiedCount`,  `InstalledTeamsCount`,  `Event Name`

From this information you can calculate key metrics:

-   Which teams (team IDs) have the Crowd Sourcer app?
-   How many users are being paired up with the Crowd Sourcer app?

 Few other kinds of events:

The  `Exceptions`  event:
- Global exceptions logging.

The  `customEvents`  event:
- CRUD operations logging.

- We provides telemetry to ascertain satisfaction through the following metrics:
	 - Clicks on no questions added
	-	Clicks on no answers added/edited/deleted
	-	Clicks on no questions answered
	-	Total and Unique Users per month
	-	Average Response Time
	-	Most/Least popular commands
	-	No of Dependency failures
