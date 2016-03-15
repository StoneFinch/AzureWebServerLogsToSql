using Stonefinch.AzureWebServerLogsToSql.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Stonefinch.AzureWebServerLogsToSql.Data
{
    public class AzureWebServerLogContext : DbContext
    {
        public AzureWebServerLogContext(string connectionString)
            : base(connectionString)
        {
            // Do not attempt to create database
            Database.SetInitializer<AzureWebServerLogContext>(null);
        }

        public DbSet<AzureWebServerLog> AzureWebServerLogs { get; set; }

        public DbSet<AzureWebServerLogFileInfo> AzureWebServerLogFileInfos { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // table names are not plural in DB, remove the convention
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
