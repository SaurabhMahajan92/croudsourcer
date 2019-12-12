# Data flow diagram to detail out the data flow between various services in crowdsourcer bot 

## Bot Installation
- When bot is installed, it checks in azure storage for knowledge base Id.
- If knowledge base is not available, qna maker API is called to create knowledge base. 
- If team Id is not available in storage table, it adds a row for team id with knowledge base id and endpoint key. 
![Ask any question](/Wiki/Images/Crowdsourcer_Installation.png)

## Ask any question
- When user asks any question, bot checks for knowledge base associated with user's team from azure storage.
- With knowledge base Id and endpoint key, question is searched in prod and test knowledge base and corresponding result is shown in card. 
![Ask any question](/Wiki/Images/Crowdsourcer_AddQuestion.png)

## Messaging Extension
- ME checks for knowledge base Id and teams Id associated with user in azure table storage.
- Knowledge base is downloaded with Qna maker API and shown in thumbnail card.
![Ask any question](/Wiki/Images/Crowdsourcer_ME.png)

## Azure function for publishing knowledge base
- Azure function is triggered every 15 min to publish knowledge base.
- It publish knowledge base only if modification is done from last publish time.    
![Publish knowledge base](/Wiki/Images/Publish_dfd.png)