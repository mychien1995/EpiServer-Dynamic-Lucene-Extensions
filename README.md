# EpiServer-Dynamic-Lucene-Extensions
EPiServer Dynamic Lucene Extensions enable you to add field dynamically and store them in Azure Blob or File System.

I built this because built in EPiServer Lucene Search only index limited amount of fields and can only be stored in physical folder.

1. You can store your indexes files in Azure Blob Storage or in File System (index will be synced using remote events)
2. Upgraded version of Azure Storage allow you to download index blob faster by seperate 1 big index files into small chunks of 1MB and download them in parallel
3. Most importantly, this library enable you to configure what EpiServer's properties that you want to index, what Content Type that you want to index instead of the hardcoded 10 ~ 12 properties that EpiServer built-in Lucene function has to offer.
