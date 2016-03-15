using System;

namespace Stonefinch.AzureWebServerLogsToSql.Models
{
    public class AzureWebServerLogFileInfo
    {
        public int AzureWebServerLogFileInfoId { get; set; }

        public string FileNameAndPath { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}
