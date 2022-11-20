using System.Net;
using DeliveryOrderProcessorInfrastucture.Models;
using Microsoft.Azure.Cosmos;

namespace DeliveryOrderProcessorInfrastucture;

public class OrderRepository
{
    public const string ContainerId = "orders";
    public const string PartitionKey = "/id";

    private readonly Database _database;
    private Container _container;
    private Container Container
    {
        get
        {
            if (_container == null)
            {
                throw new InvalidOperationException();
            }
            return _container;
        }
        init => _container = value;
    }

    public OrderRepository(Database database)
    {
        _database = database;

        Container = _database.CreateContainerIfNotExistsAsync(
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

        var res = await Container.CreateItemAsync(newItem, new PartitionKey(newItem.id));

        // if record created, will return new identifier
        if (res.StatusCode == HttpStatusCode.Created)
        {
            return newItem.id;
        }

        throw new Exception($"Order [{newItem.OrderId}] with item id {newItem.id} adding was failed");
    }
}
