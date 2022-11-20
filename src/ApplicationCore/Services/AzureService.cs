using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class AzureService : IAzureService, IDisposable
{
    private const string OrdersReservedQueueName = "ordersreserved";

    private readonly string _functionURL;
    private readonly string _functionKey;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<IAzureService> _logger;

    public AzureService(IConfiguration configuration, ILogger<IAzureService> logger, ServiceBusClient client)
    {
        _logger = logger;

        _functionURL = configuration["delivery-order-processor-url"];
        _functionKey = configuration["delivery-order-processor-key"];

        Guard.Against.Null(_functionURL);
        Guard.Against.Null(_functionKey);

        _sender = client.CreateSender(OrdersReservedQueueName);
    }
 
    /// <summary>
    /// Sends order to Order Items reserver function via Service Bus
    /// </summary>
    /// <returns></returns>
    public async Task SendToWarehouse(Order order)
    {
        try
        {
            var groupedOrder = order.OrderItems
            .Select(i => new
            {
                Quantity = i.Units,
                ItemId = i.ItemOrdered.CatalogItemId
            });
            var stringOrder = JsonExtensions.ToJson(groupedOrder);

            var message = new ServiceBusMessage(stringOrder);

            await _sender.SendMessageAsync(message);
        }
        catch(Exception ex) 
        {
            _logger.LogError("An error occurred while sending to Service Bus. Details: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Sends order to CosmosDB via Delivery Order Processor function
    /// </summary>
    /// <returns></returns>
    public async Task DeliverOrder(Order order)
    {
        try
        {
            var deliveryOrder = JsonExtensions.ToJson(order);
            await SendRequest(deliveryOrder);
        }
        catch(Exception ex)
        {
            _logger.LogError("An error occurred while sending to CosmosDB. Details: {Message}", ex.Message);
            throw;
        }
    }

    public async void Dispose()
    {
        await _sender.DisposeAsync();
    }

    private async Task SendRequest(string order)
    {
        var client = new HttpClient();

        var content = new StringContent(order);
        var request = new HttpRequestMessage(HttpMethod.Post, _functionURL)
        {
            Content = content
        };

        request.Headers.Add("x-functions-key", _functionKey);
        await client.SendAsync(request);
    }
}
