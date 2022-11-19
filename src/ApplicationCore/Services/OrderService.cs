using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Configuration;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private string _functionURL;
    private string _functionKey;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;

        InitFunctionVariables(configuration);
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);
        // deploy OrderItemsReserver before uncommenting this code
        // await SendToWarehouse(order);   

        // await SendToDeliveryOrderProcessor(order);
    }

    private void InitFunctionVariables(IConfiguration configuration)
    {
        var section = configuration.GetSection("Functions:delivery-order-processor-kba");

        _functionURL = section["FunctionURL"];
        _functionKey = section["FunctionKey"];

        //Guard.Against.Null(_functionURL);
        //Guard.Against.Null(_functionKey);
    }

    private async Task SendToWarehouse(Order order)
    {
        var groupedOrder = order.OrderItems
            .Select(i => new
            {
                Quantity = i.Units,
                ItemId = i.ItemOrdered.CatalogItemId
            });
        var stringOrder = JsonExtensions.ToJson(groupedOrder);

        await SendRequest(stringOrder);
    }

    private async Task SendToDeliveryOrderProcessor(Order order)
    {
        var deliveryOrder = JsonExtensions.ToJson(order);
        await SendRequest(deliveryOrder);
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
