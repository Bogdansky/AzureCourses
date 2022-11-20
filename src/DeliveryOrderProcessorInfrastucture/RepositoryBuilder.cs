using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using RestSharp.Extensions;

namespace DeliveryOrderProcessorInfrastucture;

public static class RepositoryBuilder
{
    public static async Task<OrderRepository> CreateOrderRepository(ILogger logger)
    {
        var endpoint = Environment.GetEnvironmentVariable("CosmosDB:CosmosEndpoint");
        var primaryKey = Environment.GetEnvironmentVariable("CosmosDB:PrimaryKey");
        var databaseId = Environment.GetEnvironmentVariable("CosmosDB:OrderDatabaseId");

        try
        {
            Guard.Against.NullOrEmpty(endpoint);
            Guard.Against.NullOrEmpty(primaryKey);
            Guard.Against.NullOrEmpty(databaseId);

            var database = await InitializeDatabaseAsync(endpoint, primaryKey, databaseId);

            return new OrderRepository(database);
        }
        catch(Exception ex) 
        {
            logger.LogError("Exception related to database initialization was arose. Details: {Message}", ex.Message);
            throw;
        }
    }

    private static async Task<Database> InitializeDatabaseAsync(string endpoint, string primaryKey, string databaseId)
    {
        var client = new CosmosClient(endpoint, primaryKey);

        return await client.CreateDatabaseIfNotExistsAsync(databaseId);
    }
}
