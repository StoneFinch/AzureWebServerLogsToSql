using Stonefinch.AzureWebServerLogsToSql.Data;
using System;

namespace Stonefinch.AzureWebServerLogsToSql.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var azureConfig = new AzureConfiguration();

            System.Console.WriteLine("AzureWebServerLogsToSql start " + DateTime.UtcNow.ToString("s"));

            // note: get these values from your publishprofile file and update the AppSettings.config file
            // ex: {REPLACE-ME}.ftp.azurewebsites.windows.net
            var ftpHost = azureConfig.GetAppSetting("FtpHost");
            var ftpUserName = azureConfig.GetAppSetting("FtpUserName");
            var ftpPassword = azureConfig.GetAppSetting("FtpPassword");

            // ex: /LogFiles/http/RawLogs/
            var ftpWebServerLogPath = azureConfig.GetAppSetting("FtpWebServerLogPath");

            // ex: "Server=.;Database=azurelogs;Integrated Security=SSPI;Connection Timeout=30;"
            var sqlConnectionString = azureConfig.GetConnectionString("AzureLogSqlConnection");

            var repository = new AzureWebServerLogRepository(sqlConnectionString);

            var service = new AzureWebServerLogsToSqlService(
                ftpHost,
                ftpUserName,
                ftpPassword,
                ftpWebServerLogPath,
                repository);

            service.SyncLogs();

            System.Console.WriteLine("AzureWebServerLogsToSql end " + DateTime.UtcNow.ToString("s"));
        }
    }
}
