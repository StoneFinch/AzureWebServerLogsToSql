CREATE TABLE [dbo].[AzureWebServerLog]
(
        AzureWebServerLogId int not null primary key identity(1, 1),
        AzureWebServerLogFileInfoId int not null,
        LogFileRowNumber int not null,
        [datetime] datetime2(2) null,
        s_sitename varchar(400) null,
        cs_method VARCHAR(15) null,
        cs_uri_stem varchar(400) null,
        cs_uri_query nvarchar(max) null,
        s_port char(6) null,
        cs_username varchar(400) null,
        c_ip varchar(50) null,
        cs_User_Agent varchar(max) null,
        cs_Cookie nvarchar(max) null,
        cs_Referer varchar(max) null,
        cs_host varchar(400) null,
        sc_status char(6) null,
        sc_substatus char(6) null,
        sc_win32_status char(6) null,
        sc_bytes int null,
        cs_bytes int null,
        time_taken int null, 
    CONSTRAINT [FK_AzureWebServerLog_AzureWebServerLogFileInfo] FOREIGN KEY (AzureWebServerLogFileInfoId) REFERENCES AzureWebServerLogFileInfo(AzureWebServerLogFileInfoId),
)

GO

CREATE INDEX [IX_AzureWebServerLog_AzureWebServerLogFileInfoId] ON [dbo].[AzureWebServerLog] (AzureWebServerLogFileInfoId)
