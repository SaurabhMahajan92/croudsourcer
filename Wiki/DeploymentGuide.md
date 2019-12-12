# Deployment guide

# Prerequisites

To begin, you will need:

-   An Azure subscription where you can create the following kinds of resources:
    -   App service
    -   App service plan
    -   Bot channels registration
    -   Azure storage account
    -   Azure search
    -   QnAMaker cognitive service
    -   Application Insights
-   A team in Microsoft Teams with your group of members. (You can add and remove team members later!)
-   A copy of the CrowdSourcer app GitHub repo ([https://github.com/OfficeDev/microsoft-teams-CrowdSourcer-app](https://github.com/OfficeDev/microsoft-teams-CrowdSourcer-app))
-   A reasonable set of Question and Answer pairs to set up the knowledge base for the bot.

# Step 1: Register Azure AD applications

Register Azure AD applications in your tenant's directory for bot.

1.  Log in to the Azure Portal for your subscription, and go to the "App registrations" blade [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps).
    
2.  Click on "New registration", and create an Azure AD application.
    
    i.  **Name**: The name of your Teams app - if you are following the template for a default deployment, we recommend "CrowdSourcer".
    ii.  **Supported account types**: Select "Accounts in any organizational directory"
    iii.  Leave the "Redirect URI" field blank.
    
    ![Azure AD App registration](/wiki/images/multitenant_app_creation.png)    
3.  Click on the "Register" button.
    
4.  When the app is registered, you'll be taken to the app's "Overview" page. Copy the **Application (client) ID** and **Directory (tenant) ID**; we will need it later. Verify that the "Supported account types" is set to **Multiple organizations**.
    
    ![Azure AD Overview page](/wiki/images/azure-config-app-step3.png)
    
5.  On the side rail in the Manage section, navigate to the "Certificates & secrets" section. In the Client secrets section, click on "+ New client secret". Add a description for the secret and select an expiry time. Click "Add".
    
    ![Azure AD Overview page](/wiki/images/multitenant_app_secret.png)
    
6.  Once the client secret is created, copy its **Value**; we will need it later.

At this point you have 3 unique values:

-   Application (client) ID for the bot
-   Client secret for the bot
-   Directory (tenant) ID

We recommend that you copy these values into a text file, using an application like Notepad. We will need these values later.

![Image](/wiki/images/multitenant_app_overview.png)

# Step 2: Deploy to your Azure subscription

1.  Click on the "Deploy to Azure" button below.

[![Deploy to Azure](https://camo.githubusercontent.com/8305b5cc13691600fbda2c857999c4153bee5e43/68747470733a2f2f617a7572656465706c6f792e6e65742f6465706c6f79627574746f6e2e706e67)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FOfficeDev%2Fmicrosoft-teams-<<To Do>>-app%2Fmaster%2FDeployment%2Fazuredeploy.json)

2.  When prompted, log in to your Azure subscription.
    
3.  Azure will create a "Custom deployment" based on the ARM template and ask you to fill in the template parameters.
    
4.  Select a subscription and resource group.
    
    -   We recommend creating a new resource group.
    -   The resource group location MUST be in a datacenter that supports: Application Insights; Azure Search; and QnA Maker. For an up-to-date list, click [here](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=logic-apps,cognitive-services,search,monitor), and select a region where the following services are available:
        -   Application Insights
        -   QnA Maker
        -   Azure Search
5.  Enter a "Base Resource Name", which the template uses to generate names for the other resources.
    
    -   The app service name `[Base Resource Name]` and cognitive service name '[Base Resource Name]-qnamaker' must be available. For example, if you select `CrowdSourcer` as the base name, `CrowdSourcer` and `CrowdSourcer-qnamaker` must be available (if already not taken); otherwise, the deployment will fail with a Conflict error.
    -   Remember the base resource name that you selected. We will need it later.
6.  Fill in the various IDs in the template:
    
    1.  **Bot Client ID**: The application (client) ID of the Microsoft Teams Bot app
    2.  **Bot Client Secret**: The client secret of the Microsoft Teams Bot app
    3.  **Tenant Id**: The tenant ID which we go while registering the AAD app.
    
    Make sure that the values are copied as-is, with no extra spaces. The template checks that GUIDs are exactly 36 characters.
    
8.  If you wish to change the app name, description, and icon from the defaults, modify the corresponding template parameters.
    
9.  Agree to the Azure terms and conditions by clicking on the check box "I agree to the terms and conditions stated above" located at the bottom of the page.
    
10.  Click on "Purchase" to start the deployment.
    
11.  Wait for the deployment to finish. You can check the progress of the deployment from the "Notifications" pane of the Azure Portal. It can take more than 10 minutes for the deployment to finish.
    
12.  Once the deployment has finished, you would be directed to a page that has the following fields:
    
    -   botId - This is the Microsoft Application ID for the CrowdSourcer bot.
    -   appDomain - This is the base domain for the CrowdSourcer Bot.

# Step 3: Setup endpoint key for app service and function app

1. Go to 'All resources' and search for '[Base Resource Name]-qnamaker' cognitive service. 

2. Inside cognitive service, select 'Keys' in 'Resource Management' section in left navigation.

3. Copy 'Key 1' or 'Key 2' value and save it somewhere.

4. Go to 'All resources' and search for '[Base Resource Name]' app service.

5. Select 'Configuration' in 'Settings' section in left navigation.

6. Click on 'QnAMakerApiEndpointKey' in 'Application Settings'.

7. Paste key value copied earlier into QnAMakerApiEndpointKey and click Ok.

8. Click 'Save'.

9. Go to 'All resources' and search for '[Base Resource Name]-function' function app.

10. Click on 'Configuration' and open 'QnaSubscriptionKey'.

11. Paste key value copied earlier into QnaSubscriptionKey and click Ok.

12. Click 'Save'.

# Step 4: Create the Teams app packages

Create Teams app packages:  To be installed to any team.

1.  Open the `Manifest\manifest.json` file in a text editor.
    
2.  Change the placeholder fields in the manifest to values appropriate for your organization.
    
    -   `developer.name` ([What's this?](https://docs.microsoft.com/en-us/microsoftteams/platform/resources/schema/manifest-schema#developer))
    -   `developer.websiteUrl`
    -   `developer.privacyUrl`
    -   `developer.termsOfUseUrl`
3.  Change the `<<botId>>` placeholder to your Azure AD application's ID from above. This is the same GUID that you entered in the template under "Bot Client ID".
    
4.  In the "validDomains" section, replace the `<<appDomain>>` with your Bot App Service's domain. This will be `[BaseResourceName].azurewebsites.net`. For example if you chose "CrowdSourcer" as the base name, change the placeholder to `CrowdSourcer.azurewebsites.net`.
    
5.  Create a ZIP package with the `manifest.json`,`color.png`, and `outline.png`. The two image files are the icons for your app in Teams.
    
    -   Name this package `CrowdSourcer.zip`.
    -   Make sure that the 3 files are the _top level_ of the ZIP package, with no nested folders.  
        ![Image5](/wiki/images/ManifestUI.png)
 

# Step 5: Run the apps in Microsoft Teams

1.  If your tenant has sideloading apps enabled, you can install your app by following the instructions [here](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/apps/apps-upload#load-your-package-into-teams)
    
2.  You can also upload it to your tenant's app catalog, so that it can be available for everyone in your tenant to install. See [here](https://docs.microsoft.com/en-us/microsoftteams/tenant-apps-catalog-teams)
    
3.  Install the app (the `CrowdSourcer.zip` package) to your team .
    
    -   We recommend using [app permission policies](https://docs.microsoft.com/en-us/microsoftteams/teams-app-permission-policies) to restrict access to this app to the members of the team.
    

# [](#troubleshooting)Troubleshooting

Please see our [Troubleshooting](Troubleshooting) page.