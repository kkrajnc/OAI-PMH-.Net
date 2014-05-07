OAI-PMH-.Net
============

OAI-PMH Data Provider and Harvester implemented as a web service with web page for easier interaction.

Technologies that ware used are .Net WebAPI, Entity Framework (EF) for data provider and harvester, 
and MVC with JavaScript for web page.

For running this solution you will need Visual Studio 2012 or later. Application is currently configured
so that it uses SQLExpress. If you use some other server please make sure that it supports Full text search
and change Web.config file appropriately. After you start the application and EF creates the database, 
you will need to run OaiPmhContextExtension.sql which will add Full-Text-Search support and some stored procedures.
