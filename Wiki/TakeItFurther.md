## Scenario 1
Questions that require filter or multi turns. Example: Same question can have different answers when asked for different geography or domain.

**Suggested Solution:**  While setting up the KB, add in relevant metadata so the KB is able to filter on the questions based on the criteria provided. Before adding Qna pair inside Knowledge base follow the steps below to add metadata for each question:
-   click on “View Options” and then select “Show metadata”.
-   Next, select the + sign to add the key-value pair.

![KB MultiTurnUI](/wiki/images/KB_MultiTurnUI.png)

For Ex: if you want the KB to be setup based on region and enable the KB answer user question depending on the user’s region, you can add the metadata as Region: US for the QnA pairs pertaining to the US region.

**Code Changes:**  Modify the app code to read data which will help to determine which QnA pairs can be retrieved from Knowledge Base depending on the filter criteria.

**Pros:**  Extensibility – Same app and knowledge base can be used for different regions, departments etc.

**Cons:**  The would be development effort costs associated to add the functionality to read user’s attributes or the data which will be used as the filter criterion.

## Scenario 2
  Customize the app to deploy for a different department. For Ex: IT aka Contoso IT Support.
  
**Suggested Solution:**  Please follow the steps as outlined below to configure the app to be used for different departments:

-   Change the text references in the associated files from Crowd Sourcer to the department to which it should cater to.

**Pros:**  Very minimal changes required.