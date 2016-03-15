using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.FtpClient;
using System.Net;
using Stonefinch.AzureWebServerLogsToSql.Models;
using Stonefinch.AzureWebServerLogsToSql.Data;

namespace Stonefinch.AzureWebServerLogsToSql
{
    public class AzureWebServerLogsToSqlService
    {
        private string FtpHost { get; set; }
        private string FtpUserName { get; set; }
        private string FtpPassword { get; set; }

        private string FtpWebServerLogPath { get; set; }

        private IAzureWebServerLogRepository AzureWebServerLogRepository { get; set; }

        public AzureWebServerLogsToSqlService(
            string ftpHost,
            string ftpUserName,
            string ftpPassword,
            string ftpWebServerLogPath,
            IAzureWebServerLogRepository azureWebServerLogRepository)
        {
            this.FtpHost = ftpHost;
            this.FtpUserName = ftpUserName;
            this.FtpPassword = ftpPassword;
            this.FtpWebServerLogPath = ftpWebServerLogPath;

            this.AzureWebServerLogRepository = azureWebServerLogRepository;
        }

        public void SyncLogs()
        {
            var ftpClient = this.CreateFtpClient();

            ftpClient.Connect();

            var ftpFilesInfos = ftpClient.GetListing(this.FtpWebServerLogPath);

            var azureWebServerLogFileInfos = this.GetWebServerLogFileInfosWithUpdates(ftpFilesInfos);

            // TODO: sync logs
        }

        private FtpClient CreateFtpClient()
        {
            var ftpClient = new FtpClient()
            {
                Host = this.FtpHost,
                Credentials = new NetworkCredential(this.FtpUserName, this.FtpPassword),
                DataConnectionEncryption = true,
                EncryptionMode = FtpEncryptionMode.Explicit
            };

            return ftpClient;
        }

        private IEnumerable<AzureWebServerLogFileInfo> GetWebServerLogFileInfosWithUpdates(FtpListItem[] allFtpListItems)
        {
            var fileInfos = this.MapFtpListItemToAzureWebServerLogFileInfos(allFtpListItems);

            var result = this.AzureWebServerLogRepository.GetFileInfosWithUpdates(fileInfos);

            return result;
        }

        private IEnumerable<AzureWebServerLogFileInfo> MapFtpListItemToAzureWebServerLogFileInfos(IEnumerable<FtpListItem> ftpListItems)
        {
            var result =
                ftpListItems
                .Select(li =>
                    new AzureWebServerLogFileInfo()
                    {
                        FileNameAndPath = li.FullName,
                        FileName = li.Name,
                        FileSize = li.Size,
                        LastModifiedDate = li.Modified
                    })
                .ToList();

            return result;
        }
    }
}
