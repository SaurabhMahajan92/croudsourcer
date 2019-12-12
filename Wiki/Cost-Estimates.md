


## Assumptions

The estimate below assumes:

-   500 users in the tenant
-   Change the text to User adds, edits or deletes 5 questions/day
-   Users use the messaging extension 25 times/week, across the entire team

## [](/wiki/costestimate#sku-recommendations)SKU recommendations

The recommended SKUs for a production environment are:

-   QnAMaker: Standard (S0)
-   App Service: Standard (S1)
-   Azure Search: Basic
    -   Create up to 14 knowledge bases
    -   The Azure Search service cannot be upgraded once it is provisioned, so select a tier that will meet your anticipated needs.

## [](/wiki/costestimate#estimated-load)Estimated load

**Number of QnA queries**: 500 users * 5 questions/user/day * 30 (number of days in a month) = 75000 questions/month

**Data storage**: 1 GB max

**Table data operations**:

    -   Storage is called to check knowledge base Id on each qna api call.
    -   Total number of read calls in storage = Number of Qna queries = 75000 reads
    -   Total number of write calls in storage = Number of teams in which app is installed = 20 approx.

## [](/wiki/costestimate#estimated-cost)Estimated cost

**IMPORTANT:**  This is only an estimate, based on the assumptions above. Your actual costs may vary.

Prices were taken from the  [Azure Pricing Overview](https://azure.microsoft.com/en-us/pricing/)  on 27 November 2019, for the West US 2 region.

Use the  [Azure Pricing Calculator](https://azure.com/e/d95f6192b2a34d3c847750c90ac0648c)  to model different service tiers and usage patterns.


|  Resource |  Tier |  Load |  Monthly price |   
|---|---|---|---|
|  Storage account (Table)| Standard_LRS|< 1GB data, 75,000 operations|  $0.05 + $0.01 = $0.06 |
|  Bot Channels Registration | F0  |  N/A | Free  |
|  App Service Plan | S1  | 744 hours  | $74.40  |
|  App Service (Messaging Extension)| -|  |(charged to App Service Plan)|
|  Application Insights (Messaging Extension) | -  |  < 5GB data | (free up to 5 GB)|
|QnAMaker Cognitive Service|S0||$10|
|Azure Search|B||$75.14|
|App Service (QnAMaker)|F0||(charged to App Service Plan)|
|Application Insights (QnAMaker)||< 5GB data|(free up to 5 GB)|
|Azure Function|Dedicated|3000 executions|(free up to 1 million executions)|
|**Total**|||**$159.60**|