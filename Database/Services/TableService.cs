using Azure.Data.Tables;
using Configuration.Sections;
using Database.Helpers;
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
                if (!tableName.IsTable()) throw new ArgumentException($"Error: '{tableName}' is not a valid Table.");

                return new TableClient(_accountUri, name, azureAdCfg.GetClientSecretCredential());
            });

        public TableClient GetTable(Table table)
        {
            return GetTable(table.ToString());
        }
    }
}
