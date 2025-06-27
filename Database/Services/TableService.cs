using Azure.Data.Tables;
using Database.Helpers;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace Database.Services
{
    public sealed class TableService : ITableService
    {
        private readonly string _connString;
        private readonly ConcurrentDictionary<string, TableClient> _cache = new();

        public TableService(IConfiguration cfg)
            => _connString = cfg.GetConnectionString("Storage");

        public TableClient GetTable(string tableName)
            => _cache.GetOrAdd(tableName, name =>
            {
                if (!tableName.IsTable()) throw new ArgumentException($"Error: '{tableName}' is not a valid Table.");

                var client = new TableClient(_connString, name);
                client.CreateIfNotExists();
                return client;
            });

        public TableClient GetTable(Table table)
        {
            return GetTable(table.ToString());
        }
    }
}
