using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using DeliveryOrderProcessorInfrastucture;
using DeliveryOrderProcessorInfrastucture.Models;

namespace DeliveryOrderProcessor;

public static class DeliveryOrderProcessor
{
    [FunctionName("DeliveryOrderProcessor")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        var order = await GetOrderRequest(req.Body);

        var repository = await RepositoryBuilder.CreateOrderRepository();
        await repository.AddOrderAsync(order);

        log.LogInformation($"Order {order.Id} was added at CosmosDB");
        return new OkObjectResult("Hello world");
    }

    private static async Task<Order> GetOrderRequest(Stream body)
    {
        var buffer = new byte[body.Length];
        await body.ReadAsync(buffer);

        var stringOrder = Encoding.Default.GetString(buffer, 0, buffer.Length);

        return JsonConvert.DeserializeObject<Order>(stringOrder);
    }
}
