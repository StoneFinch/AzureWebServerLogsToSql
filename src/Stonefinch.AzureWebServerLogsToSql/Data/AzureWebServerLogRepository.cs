using EntityFramework.BulkInsert.Extensions;
using Stonefinch.AzureWebServerLogsToSql.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Stonefinch.AzureWebServerLogsToSql.Data
{
    public interface IAzureWebServerLogRepository
    {
        IEnumerable<AzureWebServerLogFileInfo> GetFileInfosWithUpdates(IEnumerable<AzureWebServerLogFileInfo> fileInfos);

        IEnumerable<AzureWebServerLogFileInfo> InsertAzureWebServerLogFileInfos(IEnumerable<AzureWebServerLogFileInfo> fileInfos);

        IEnumerable<AzureWebServerLog> ReplaceAllAzureWebServerLogsForFileInfoId(AzureWebServerLogFileInfo fileInfo, IEnumerable<AzureWebServerLog> azureWebServerLogs);
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

        private AzureWebServerLogContext CreateAzureWebServerLogContext()
        {
            return new AzureWebServerLogContext(this.sqlConnectionString);
        }
    }
}
