
# Data stores

The app uses the following data stores:  
1. Azure Storage Account  
- [Table] Storage for teams & knowledge base (KB) related information like rowkey (team id), KbId, and  kb endpoint key.

All these resources are created in your Azure subscription. None are hosted directly by Microsoft.

# [](/wiki/Datastore#storage-account)Storage account

## [](/wiki/Datastore#configurationinfo-table)CrowdSourcer Table

The **crowdsourcer** table stores data about the teams & knowledge base for the bot. The table has the following rows.

| Attribute | Comment |  
|--|--|  
|PartitionKey |This represents the partition key of the azure storage table|  
|RowKey| Represents the unique id of each row|  
|Timestamp| Contains the Date time of row creation|  
|EndpointKey| Contains the KB endpoint |  
|KBID| Contains the KBID of knowledge base |