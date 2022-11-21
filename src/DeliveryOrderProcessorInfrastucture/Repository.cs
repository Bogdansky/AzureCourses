using System.Net;
using Ardalis.GuardClauses;
using DeliveryOrderProcessorInfrastucture.Models;
using Microsoft.Azure.Cosmos;

namespace DeliveryOrderProcessorInfrastucture;

public class OrderRepository
{
    public const string ContainerId = "orders";
    public const string PartitionKey = "/id";

    private readonly Database _database;
    private readonly Container _container;

    public OrderRepository(Database database)
    {
        Guard.Against.Null(database);

        _database = database;

        _container = _database.CreateContainerIfNotExistsAsync(
                id: ContainerId,
                partitionKeyPath: PartitionKey)
            .GetAwaiter().GetResult();
    }

    public async Task<string> AddOrderAsync(Order order)
    {
        var newItem = new OrderCosmos
        {
            id = Guid.NewGuid().ToString(),
            OrderId = order.Id,
            ShipToAddress = order.ShipToAddress,
            OrderItems = order.OrderItems,
            TotalPrice = order.OrderItems.Sum(x => x.Units * x.UnitPrice)
        };

        var res = await _container.CreateItemAsync(newItem, new PartitionKey(newItem.id));

        // if record created, will return new identifier
        if (res.StatusCode == HttpStatusCode.Created)
        {
            return newItem.id;
        }

        throw new Exception($"Order [{newItem.OrderId}] with item id {newItem.id} adding was failed");
    }
}
