CREATE TABLE [dbo].[AzureWebServerLogFileInfo]
(
    AzureWebServerLogFileInfoId int not null primary key identity(1, 1),
    FileNameAndPath varchar(200) not null,
    FileName varchar(200) not null,
    FileSize bigint not null,
    LastModifiedDate datetime2(2) not null
)

GO

CREATE INDEX [IX_AzureWebServerLogFileInfo_FileNameAndPath] ON [dbo].[AzureWebServerLogFileInfo] (FileNameAndPath)
