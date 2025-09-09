using Azure.Data.Tables;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.Entities;
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

        private static TableName GetTableName<T>() where T : BaseEntity
        {
            var method = typeof(T).GetMethod("GetTable") ?? throw new InvalidOperationException($"Method 'GetTable' not found on type {typeof(T).Name}");

            if (Activator.CreateInstance(typeof(T)) is not BaseEntity instance)
                throw new InvalidOperationException($"Could not create instance of type {typeof(T).Name}");

            var tableObj = method.Invoke(instance, null);
            if (tableObj is not TableName tableName)
                throw new InvalidOperationException($"Returned value from 'GetTable' is not of type TableName");

            return tableName;
        }

        public async Task<List<T>> GetTableEntries<T>() where T : BaseEntity
        {
            TableName tableName = GetTableName<T>();

            var entities = new List<T>();
            await foreach (var entity in GetTable(tableName).QueryAsync<T>())
            {
                entities.Add(entity);
            }

            return entities;
        }

        public async Task<T?> GetTableEntry<T>(string partitionKey, string rowKey) where T : BaseEntity
        {
            var table = GetTable(GetTableName<T>());

            try
            {
                var entity = await table.GetEntityAsync<T>(partitionKey, rowKey);
                return entity.Value;
            }
            catch (Azure.RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    return null;
                }

                throw;
            }

        }
    }
}
