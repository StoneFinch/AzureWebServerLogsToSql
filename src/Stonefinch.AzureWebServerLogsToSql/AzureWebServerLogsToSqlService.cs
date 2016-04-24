using DataAccess;
using Stonefinch.AzureWebServerLogsToSql.Data;
using Stonefinch.AzureWebServerLogsToSql.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.FtpClient;

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
            Console.WriteLine("Syncing Logs.");

            var ftpFilesInfos = this.GetFtpListItems();

            // find all file infos that potentially contain new data
            Console.WriteLine("Determining files that have updates.");
            var azureWebServerLogFileInfosWithUpdates = this.GetWebServerLogFileInfosWithUpdates(ftpFilesInfos);

            Console.WriteLine("Files with updates: " + azureWebServerLogFileInfosWithUpdates.Count().ToString());

            // insert all file infos we have not seen before (id = 0)
            // TODO: if there is a failure on the first bulk insert of log data, and the file never changes, manual cleanup will be required, since these logs will be filtered out of future runs
            this.AzureWebServerLogRepository
                .InsertAzureWebServerLogFileInfos(
                    azureWebServerLogFileInfosWithUpdates
                        .Where(fi => fi.AzureWebServerLogFileInfoId == 0)
                        .ToList());

            Console.WriteLine("Updating SQL log table for files with updates.");

            // TODO: parallel
            foreach (var fileInfo in azureWebServerLogFileInfosWithUpdates)
            {
                try
                {
                    Console.WriteLine($"Updating for: {fileInfo.FileNameAndPath}; {fileInfo.LastModifiedDate.ToString("s")}; {fileInfo.FileSize}.");
                    this.UpdateAzureWebServerLogsForAzureWebServerLogFileInfo(fileInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while processing file: {fileInfo.FileNameAndPath}; {ex.ToString()}");
                }
            }

            Console.WriteLine("Sync logs complete.");
        }

        private IEnumerable<FtpListItem> GetFtpListItems()
        {
            Console.WriteLine("Connecting to FTP Server.");
            var ftpClient = this.CreateFtpClient();

            ftpClient.Connect();

            Console.WriteLine("Getting file listing from FTP Server.");
            var ftpFilesInfos = ftpClient.GetListing(this.FtpWebServerLogPath).OrderBy(x => x.Modified).ToList();

            return ftpFilesInfos;
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

        private IEnumerable<AzureWebServerLogFileInfo> GetWebServerLogFileInfosWithUpdates(IEnumerable<FtpListItem> allFtpListItems)
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

        private void UpdateAzureWebServerLogsForAzureWebServerLogFileInfo(AzureWebServerLogFileInfo fileInfo)
        {
            var ftpClient = this.CreateFtpClient();
            ftpClient.Connect();

            // retrieve file content
            Console.WriteLine("Retrieving File Content from FTP Server.");
            var fileContent = this.GetFileContent(ftpClient, fileInfo.FileNameAndPath);

            Console.WriteLine("File Content retrieved. Parsing file.");

            // remove entire first line and "#Fields: " prefix of header row
            var headerRowStartIndex = fileContent.IndexOf(@"#Fields: ");
            fileContent = fileContent.Substring(headerRowStartIndex + 9);

            // File is space delimited. Replace all `,` with `_` and then replace all ` ` with `,`
            fileContent = fileContent.Replace(",", "_").Replace(" ", ",");

            var dt = DataTable.New.ReadFromString(fileContent);

            var azureWebServerLogs = this.MapDataTableToAzureWebServerLogs(dt, fileInfo.AzureWebServerLogFileInfoId);

            Console.WriteLine($"File parsed. Records found: {azureWebServerLogs.Count().ToString()}. Updating SQL Table.");
            this.AzureWebServerLogRepository.ReplaceAllAzureWebServerLogsForFileInfoId(fileInfo, azureWebServerLogs);
            Console.WriteLine($"SQL Update complete.");
        }

        private string GetFileContent(FtpClient ftpClient, string fileNameAndPath)
        {
            var result = "";

            using (var ftpStream = ftpClient.OpenRead(fileNameAndPath))
            using (var memoryStream = new MemoryStream())
            {
                ftpStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                var sr = new StreamReader(memoryStream);
                result = sr.ReadToEnd();
            }

            return result;
        }

        private IEnumerable<AzureWebServerLog> MapDataTableToAzureWebServerLogs(MutableDataTable logfile, int azureWebServerLogFileInfoId)
        {
            var result = new List<AzureWebServerLog>();

            var rows = logfile.Rows.ToList();

            // TODO: parallel
            for (int i = 0; i < rows.Count; i++)
            {
                var wsl = rows[i];

                var l = new AzureWebServerLog();

                l.AzureWebServerLogFileInfoId = azureWebServerLogFileInfoId;
                l.LogFileRowNumber = i;

                l.cs_bytes = this.SafeParseInt(wsl["cs-bytes"]);
                l.cs_Cookie = wsl["cs(Cookie)"];
                l.cs_host = wsl["cs-host"];
                l.cs_method = wsl["cs-method"];
                l.cs_Referer = wsl["cs(Referer)"];
                l.cs_uri_query = wsl["cs-uri-query"];
                l.cs_uri_stem = wsl["cs-uri-stem"];
                l.cs_username = wsl["cs-username"];
                l.cs_User_Agent = wsl["cs(User-Agent)"];
                l.c_ip = wsl["c-ip"];

                l.datetime = this.SafeParseDateTime(wsl["date"], wsl["time"]);

                l.sc_bytes = this.SafeParseInt(wsl["sc-bytes"]);
                l.sc_status = wsl["sc-status"];
                l.sc_substatus = wsl["sc-substatus"];
                l.sc_win32_status = wsl["sc-win32-status"];
                l.s_port = wsl["s-port"];
                l.s_sitename = wsl["s-sitename"];
                l.time_taken = this.SafeParseInt(wsl["time-taken"]);

                result.Add(l);
            }
            
            return result;
        }

        private DateTime SafeParseDateTime(string date, string time)
        {
            var result = new DateTime(1900, 1, 1);

            if (String.IsNullOrWhiteSpace(date) || String.IsNullOrWhiteSpace(time))
                return result;

            if (DateTime.TryParse($"{date}T{time}", out result))
                return result;

            return result;
        }

        private int SafeParseInt(string val)
        {
            int result = 0;

            if (String.IsNullOrWhiteSpace(val))
                return result;
            
            if (int.TryParse(val, out result))
                return result;

            return result;
        }
    }
}
