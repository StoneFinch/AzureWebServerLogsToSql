##Azure HTTP Web Server Logs to SQL

If you enable File System Web Server logs for your Azure App Service...

*Settings > Diagnostics logs > Web server logging > File System*

![image](https://cloud.githubusercontent.com/assets/109249/13767635/88ef592c-ea3a-11e5-8fa3-990356ccf73e.png)

...You'll start to see logs on the file system for your app service here:

(you can find your FTP host and credentials in the publish profile file)

*/LogFiles/http/RawLogs*

![image](https://cloud.githubusercontent.com/assets/109249/13767646/a9ec7182-ea3a-11e5-87fa-f76ba5385324.png)

Alternatively, you can see these logs through the Kudu UI:

https://{yourappservicename}.scm.azurewebsites.net/DebugConsole/?shell=powershell

![image](https://cloud.githubusercontent.com/assets/109249/13767652/b84d205a-ea3a-11e5-8448-b0a62ba77b60.png)

Instead of downloading these one and a time and parsing through them, you can use this library to assist in loading them into a relational database.

You could even run this as a webjob within your app service.

###Setup and Usage
 1. Clone the repo
 2. Create necessary [SQL Tables](https://github.com/StoneFinch/AzureWebServerLogsToSql/blob/master/scripts/create_azurelogs_tables.sql)
 3. Set appropriate values in [AppSettings.config](https://github.com/StoneFinch/AzureWebServerLogsToSql/blob/master/src/Stonefinch.AzureWebServerLogsToSql.Web/AppSettings.config) and [ConnectionStrings.config](https://github.com/StoneFinch/AzureWebServerLogsToSql/blob/master/src/Stonefinch.AzureWebServerLogsToSql.Web/ConnectionStrings.config)
  - note: The AppSettings.config and ConnectionStrings.config files are [referenced by the console application](https://github.com/StoneFinch/AzureWebServerLogsToSql/blob/master/src/Stonefinch.AzureWebServerLogsToSql.Console/Stonefinch.AzureWebServerLogsToSql.Console.csproj#L60) as Linked Files. Update the vaules in the Web project, and after a successful build they will be included in the Console project's build output directory.
 4. Run console application project OR deploy Web Project to Azure Web App/App Service (the console app is [defined as a WebJob](https://github.com/StoneFinch/AzureWebServerLogsToSql/blob/master/src/Stonefinch.AzureWebServerLogsToSql.Web/Properties/webjobs-list.json) and will be deployed with the Web project, and is [scheduled to run every 15 minutes](https://github.com/StoneFinch/AzureWebServerLogsToSql/blob/master/src/Stonefinch.AzureWebServerLogsToSql.Console/Properties/webjob-publish-settings.json)).

###Roadmap
 - currently empty
