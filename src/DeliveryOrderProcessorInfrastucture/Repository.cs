using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeliveryOrderProcessorInfrastucture.Models;
using Microsoft.Azure.Cosmos;

namespace DeliveryOrderProcessorInfrastucture;

public class OrderRepository
{
    public const string ContainerId = "orders";
    public const string PartitionKey = "/id";

    private Database _database;
    private Container _container;

    public OrderRepository(Database database)
    {
        _database = database;
    }

    public async Task<string> AddOrderAsync(Order order)
    {
        var container = await GetContainerAsync();

        var newItem = new OrderCosmos
        {
            id = Guid.NewGuid().ToString(),
            OrderId = order.Id,
            ShipToAddress = order.ShipToAddress,
            OrderItems = order.OrderItems,
            TotalPrice = order.OrderItems.Sum(x => x.Units * x.UnitPrice)
        };

        var res = await container.CreateItemAsync(newItem, new PartitionKey(newItem.id));

        if (res.StatusCode == System.Net.HttpStatusCode.Created)
        {
            return newItem.id;
        }

        throw new Exception($"Order [{newItem.OrderId}] with item id {newItem.id} adding was failed");
    }

    public async Task DeleteOrder(string itemId) 
    {
        var container = await GetContainerAsync();
        throw new NotImplementedException();
    }

    private async Task<Container> GetContainerAsync()
    {
        if (_container != null)
        {
            return _container;
        }

        _container = await _database.CreateContainerIfNotExistsAsync(
            id: ContainerId, 
            partitionKeyPath: PartitionKey
        );
        return _container;
    }
}
