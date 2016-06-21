using EntityFramework.BulkInsert.Extensions;
using Stonefinch.AzureWebServerLogsToSql.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Stonefinch.AzureWebServerLogsToSql.Data
{
    public interface IAzureWebServerLogRepository
    {
        IEnumerable<AzureWebServerLogFileInfo> GetFileInfosWithUpdates(IEnumerable<AzureWebServerLogFileInfo> fileInfos);

        IEnumerable<AzureWebServerLogFileInfo> InsertAzureWebServerLogFileInfos(IEnumerable<AzureWebServerLogFileInfo> fileInfos);

        IEnumerable<AzureWebServerLog> ReplaceAllAzureWebServerLogsForFileInfoId(AzureWebServerLogFileInfo fileInfo, IEnumerable<AzureWebServerLog> azureWebServerLogs);

        /// <summary>
        /// Update AzureWebServerLogFileInfo table with latest info 
        /// and attempts to insert only new records from the IIS Log Flat File into the AzureWebServerLog table.
        /// Uses the AzureWebServerLogFileInfoId and LogFileRowNumber columns/properties to determine uniqueness.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="azureWebServerLogs"></param>
        /// <returns></returns>
        IEnumerable<AzureWebServerLog> InsertNewAzureWebServerLogsForFileInfoId(AzureWebServerLogFileInfo fileInfo, IEnumerable<AzureWebServerLog> azureWebServerLogs);
    }

    public class AzureWebServerLogRepository : IAzureWebServerLogRepository
    {
        private string sqlConnectionString { get; set; }

        public AzureWebServerLogRepository(string sqlConnectionString)
        {
            this.sqlConnectionString = sqlConnectionString;
        }

        public IEnumerable<AzureWebServerLogFileInfo> GetFileInfosWithUpdates(IEnumerable<AzureWebServerLogFileInfo> fileInfos)
        {
            var result = new List<AzureWebServerLogFileInfo>();

            using (var ctx = this.CreateAzureWebServerLogContext())
            {
                // filesInfos should have unique fileNames
                var fileInfosDict = fileInfos.ToDictionary(x => x.FileNameAndPath);

                // get records from DB with info on the last time these log files were processed
                var fileInfosFromDb =
                    (from fi in ctx.AzureWebServerLogFileInfos
                     where fileInfosDict.Keys.Contains(fi.FileNameAndPath)
                     select fi).ToDictionary(x => x.FileNameAndPath);

                // determine log files that are new, or that have changed since the last time we processed them
                foreach (var fileInfo in fileInfosDict.Values)
                {
                    // if the file already existed in the DB, determine if it has changed.
                    if (fileInfosFromDb.ContainsKey(fileInfo.FileNameAndPath))
                    {
                        var fileInfoFromDb = fileInfosFromDb[fileInfo.FileNameAndPath];

                        // determine if the file provided has changes from the record we have
                        if (fileInfoFromDb.FileSize != fileInfo.FileSize
                            || fileInfoFromDb.LastModifiedDate != fileInfo.LastModifiedDate)
                        {
                            // changes identified, add to result set (set primary key from DB)
                            fileInfo.AzureWebServerLogFileInfoId = fileInfoFromDb.AzureWebServerLogFileInfoId;
                            result.Add(fileInfo);
                        }
                        else
                        {
                            // this file has no changes, do nothing
                        }
                    }
                    else
                    {
                        // we have no record of the file in the DB, it must be new
                        result.Add(fileInfo);
                    }
                }
            }

            return result;
        }

        public IEnumerable<AzureWebServerLogFileInfo> InsertAzureWebServerLogFileInfos(IEnumerable<AzureWebServerLogFileInfo> fileInfos)
        {
            if (fileInfos == null || fileInfos.Count() == 0)
                return fileInfos;

            using (var ctx = this.CreateAzureWebServerLogContext())
            {
                ctx.AzureWebServerLogFileInfos.AddRange(fileInfos);
                ctx.SaveChanges();
            }

            return fileInfos;
        }

        public IEnumerable<AzureWebServerLog> ReplaceAllAzureWebServerLogsForFileInfoId(AzureWebServerLogFileInfo fileInfo, IEnumerable<AzureWebServerLog> azureWebServerLogs)
        {
            using (var ctx = this.CreateAzureWebServerLogContext())
            {
                // NOTE: bulkInsert no longer appears to work with new Database.BeginTransaction() methods
                ////using (var trans = ctx.Database.BeginTransaction())
                ////{
                try
                {
                    // delete all existing logs
                    ctx.Database.ExecuteSqlCommand("delete from dbo.AzureWebServerLog where AzureWebServerLogFileInfoId = @Id", new SqlParameter("@Id", fileInfo.AzureWebServerLogFileInfoId));

                    // update fileInfo values
                    var fileInfoFromDb = ctx.AzureWebServerLogFileInfos.Single(fi => fi.AzureWebServerLogFileInfoId == fileInfo.AzureWebServerLogFileInfoId);
                    fileInfoFromDb.LastModifiedDate = fileInfo.LastModifiedDate;
                    fileInfoFromDb.FileSize = fileInfo.FileSize;

                    // insert all AzureWebServerLogs
                    // NOTE: bulkInsert no longer appears to work with new Database.BeginTransaction() methods
                    ctx.BulkInsert(azureWebServerLogs, 500);

                    // save
                    ctx.SaveChanges();

                    ////trans.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while processing fileInfo: {fileInfo.AzureWebServerLogFileInfoId}; ex: {ex.ToString()}");

                    ////trans.Rollback();
                }
            }

            return azureWebServerLogs;
        }

        public IEnumerable<AzureWebServerLog> InsertNewAzureWebServerLogsForFileInfoId(AzureWebServerLogFileInfo fileInfo, IEnumerable<AzureWebServerLog> azureWebServerLogs)
        {
            var dataTable = azureWebServerLogs.ToDataTable();

            using (var sqlConnection = new SqlConnection(this.sqlConnectionString))
            {
                sqlConnection.Open();
                var sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);

                var updateFileInfoSql =
@"update dbo.AzureWebServerLogFileInfo
set LastModifiedDate = @LastModifiedDate
,FileSize = @FileSize
where AzureWebServerLogFileInfoId = @AzureWebServerLogFileInfoId";
                var updateFileInfoSqlCommand = new SqlCommand(updateFileInfoSql, sqlConnection, sqlTransaction);
                updateFileInfoSqlCommand.Parameters.AddWithValue("LastModifiedDate", fileInfo.LastModifiedDate);
                updateFileInfoSqlCommand.Parameters.AddWithValue("FileSize", fileInfo.FileSize);
                updateFileInfoSqlCommand.Parameters.AddWithValue("AzureWebServerLogFileInfoId", fileInfo.AzureWebServerLogFileInfoId);
                updateFileInfoSqlCommand.ExecuteNonQuery();

                var tempTableName = "#AzureWebServerLogTemp";
                // create our temp table to hold logs before we perform set insert operation
                var createTablesql = $"create table {tempTableName} ([AzureWebServerLogId] [int] NOT NULL, [AzureWebServerLogFileInfoId] [int] NOT NULL,[LogFileRowNumber] [int] NOT NULL,[datetime] [datetime2](2) NULL,[s_sitename] [varchar](400) NULL,[cs_method] [varchar](15) NULL,[cs_uri_stem] [varchar](400) NULL,[cs_uri_query] [nvarchar](max) NULL,[s_port] [char](6) NULL,[cs_username] [varchar](400) NULL,[c_ip] [varchar](50) NULL,[cs_User_Agent] [varchar](max) NULL,[cs_Cookie] [nvarchar](max) NULL,[cs_Referer] [varchar](max) NULL,[cs_host] [varchar](400) NULL,[sc_status] [char](6) NULL,[sc_substatus] [char](6) NULL,[sc_win32_status] [char](6) NULL,[sc_bytes] [int] NULL,[cs_bytes] [int] NULL,[time_taken] [int] NULL)";
                var createTableSqlCommand = new SqlCommand(createTablesql, sqlConnection, sqlTransaction);
                createTableSqlCommand.ExecuteNonQuery();

                var sbc = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction)
                {
                    BatchSize = 500,
                    DestinationTableName = tempTableName,
                    BulkCopyTimeout = 60
                };

                sbc.WriteToServer(dataTable);

                var insertLogsSql =
@"insert into dbo.AzureWebServerLog (AzureWebServerLogFileInfoId, LogFileRowNumber, datetime, s_sitename, cs_method, cs_uri_stem, cs_uri_query, s_port, cs_username, c_ip, cs_User_Agent, cs_Cookie, cs_Referer, cs_host, sc_status, sc_substatus, sc_win32_status, sc_bytes, cs_bytes, time_taken)
select t.AzureWebServerLogFileInfoId, t.LogFileRowNumber, t.datetime, t.s_sitename, t.cs_method, t.cs_uri_stem, t.cs_uri_query, t.s_port, t.cs_username, t.c_ip, t.cs_User_Agent, t.cs_Cookie, t.cs_Referer, t.cs_host, t.sc_status, t.sc_substatus, t.sc_win32_status, t.sc_bytes, t.cs_bytes, t.time_taken
from
#AzureWebServerLogTemp t
left join dbo.AzureWebServerLog l on (t.AzureWebServerLogFileInfoId = l.AzureWebServerLogFileInfoId and t.LogFileRowNumber = l.LogFileRowNumber)
where
l.AzureWebServerLogFileInfoId is null";

                var insertLogsSqlCommand = new SqlCommand(insertLogsSql, sqlConnection, sqlTransaction) { CommandTimeout = 120 };
                insertLogsSqlCommand.ExecuteNonQuery();

                var cleanupSql = $"drop table {tempTableName}";
                var cleanupSqlCommand = new SqlCommand(cleanupSql, sqlConnection, sqlTransaction);
                cleanupSqlCommand.ExecuteNonQuery();

                sqlTransaction.Commit();
            }

            return azureWebServerLogs;
        }

        private AzureWebServerLogContext CreateAzureWebServerLogContext()
        {
            return new AzureWebServerLogContext(this.sqlConnectionString);
        }
    }
}
