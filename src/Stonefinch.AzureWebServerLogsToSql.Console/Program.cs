using Stonefinch.AzureWebServerLogsToSql.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stonefinch.AzureWebServerLogsToSql.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            // note: get these values from your publishprofile file
            var ftpHost = @"{REPLACE-ME}.ftp.azurewebsites.windows.net";
            var ftpUserName = @"";
            var ftpPassword = @"";

            var ftpWebServerLogPath = @"/LogFiles/http/RawLogs/";

            // ex: "Server=.;Database=azurelogs;Integrated Security=SSPI;Connection Timeout=30;"
            var sqlConnectionString = "";

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
