using System.Collections.Generic;
using System.Data;

namespace Stonefinch.AzureWebServerLogsToSql
{
    public static class IEnumerableExtensions
    {
        public static DataTable ToDataTable<T>(this IEnumerable<T> list)
        {
            var dataTable = CreateDataTable<T>();
            var entityType = typeof(T);
            var properties = entityType.GetProperties();

            foreach (var item in list)
            {
                var dataRow = dataTable.NewRow();

                foreach (var prop in properties)
                {
                    dataRow[prop.Name] = prop.GetValue(item);
                }

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        private static DataTable CreateDataTable<T>()
        {
            var entityType = typeof(T);
            var dataTable = new DataTable(entityType.Name);
            var properties = entityType.GetProperties();

            foreach (var prop in properties)
            {
                dataTable.Columns.Add(prop.Name, prop.PropertyType);
            }

            return dataTable;
        }
    }
}
