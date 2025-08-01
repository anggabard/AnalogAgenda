using Azure.Data.Tables;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.Helpers;
using Database.Services.Interfaces;
using System.Collections.Concurrent;

namespace Database.Services
{
    public sealed class TableService(AzureAd azureAdCfg, Storage storageCfg) : ITableService
    {
        private readonly Uri _accountUri = new($"https://{storageCfg.AccountName}.table.core.windows.net");
        private readonly ConcurrentDictionary<string, TableClient> _cache = new();

        public TableClient GetTable(string tableName)
            => _cache.GetOrAdd(tableName, name =>
            {
                if (!name.IsTable()) throw new ArgumentException($"Error: '{name}' is not a valid Table.");

                return new TableClient(_accountUri, name, azureAdCfg.GetClientSecretCredential());
            });

        public TableClient GetTable(TableName table)
        {
            return GetTable(table.ToString());
        }
    }
}
