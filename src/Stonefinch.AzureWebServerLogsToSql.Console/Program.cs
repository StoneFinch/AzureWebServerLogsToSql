using Stonefinch.AzureWebServerLogsToSql.Data;
using System.Configuration;

namespace Stonefinch.AzureWebServerLogsToSql.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            // note: get these values from your publishprofile file and update the AppSettings.config file
            // ex: {REPLACE-ME}.ftp.azurewebsites.windows.net
            var ftpHost = ConfigurationManager.AppSettings["FtpHost"];
            var ftpUserName = ConfigurationManager.AppSettings["FtpUserName"];
            var ftpPassword = ConfigurationManager.AppSettings["FtpPassword"];

            // ex: /LogFiles/http/RawLogs/
            var ftpWebServerLogPath = ConfigurationManager.AppSettings["FtpWebServerLogPath"];

            // ex: "Server=.;Database=azurelogs;Integrated Security=SSPI;Connection Timeout=30;"
            var sqlConnectionString = ConfigurationManager.ConnectionStrings["AzureLogSqlConnection"].ConnectionString;

            var repository = new AzureWebServerLogRepository(sqlConnectionString);

            var service = new AzureWebServerLogsToSqlService(
                ftpHost,
                ftpUserName,
                ftpPassword,
                ftpWebServerLogPath,
                repository);

            service.SyncLogs();
        }
    }
}
