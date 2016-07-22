using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stonefinch.AzureWebServerLogsToSql.Console
{
    public interface IAzureConfiguration
    {
        string GetConnectionString(string key);

        string GetAppSetting(string key);
    }

    /// <summary>
    /// Class to assist in retrieving app settings if using common azure deployment/config methods.
    /// App settings will be stored as environment variables and will be overwritten based on azure portal settings.
    /// </summary>
    public class AzureConfiguration : IAzureConfiguration
    {
        private const string AzureEnvironmentVariableConnectionStringPrefix1 = "CUSTOMCONNSTR_";
        private const string AzureEnvironmentVariableConnectionStringPrefix2 = "SQLAZURECONNSTR_";
        private const string AzureEnvironmentVariableAppSettingPrefix = "APPSETTING_";

        public string GetConnectionString(string key)
        {
            // Attempt to retrieve environment variable first, then app setting from config file.
            return
                new[]
                {
                    TryGetEnvironmentVariable(AzureEnvironmentVariableConnectionStringPrefix1, key),
                    TryGetEnvironmentVariable(AzureEnvironmentVariableConnectionStringPrefix2, key),
                    TryGetConnectionString(key)
                }.FirstOrDefault(x => !String.IsNullOrWhiteSpace(x));
        }

        public string GetAppSetting(string key)
        {
            return
                new[]
                {
                    TryGetEnvironmentVariable(AzureEnvironmentVariableAppSettingPrefix, key),
                    TryGetAppSetting(key)
                }.FirstOrDefault(x => !String.IsNullOrWhiteSpace(x));
        }

        private string TryGetEnvironmentVariable(string prefix, string key)
        {
            return Environment.GetEnvironmentVariable($"{prefix}{key}");
        }

        private string TryGetConnectionString(string key)
        {
            return ConfigurationManager.ConnectionStrings[key]?.ConnectionString;
        }

        private string TryGetAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
