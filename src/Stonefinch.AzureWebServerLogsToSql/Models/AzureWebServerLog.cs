using System;

namespace Stonefinch.AzureWebServerLogsToSql.Models
{
    public class AzureWebServerLog
    {
        public int AzureWebServerLogId { get; set; }

        public int AzureWebServerLogFileInfoId { get; set; }

        public int LogFileRowNumber { get; set; }

        public DateTime datetime { get; set; }
        public string s_sitename { get; set; }
        public string cs_method { get; set; }
        public string cs_uri_stem { get; set; }
        public string cs_uri_query { get; set; }
        public string s_port { get; set; }
        public string cs_username { get; set; }
        public string c_ip { get; set; }
        public string cs_User_Agent { get; set; }
        public string cs_Cookie { get; set; }
        public string cs_Referer { get; set; }
        public string cs_host { get; set; }
        public string sc_status { get; set; }
        public string sc_substatus { get; set; }
        public string sc_win32_status { get; set; }
        public int sc_bytes { get; set; }
        public int cs_bytes { get; set; }
        public int time_taken { get; set; }
    }
}
