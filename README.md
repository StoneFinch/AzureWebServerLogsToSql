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


###Roadmap
 - create webjob wrapper for library
 - create powershell command line script wrapper
