using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderFunctions.Models;

namespace OrderFunctions;

public class OrderItemsReserver
{
    private static readonly IConfiguration _config;

    static OrderItemsReserver()
    {
        _config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
    }

    [FixedDelayRetry(3, "00:00:10")]
    [FunctionName("OrderItemsReserver")]
    public static void Run([ServiceBusTrigger("ordersreserved", Connection = "ServiceBusConnectionString")] string queueItem, ILogger log)
    {
        try
        {
            SendItemToBlob(queueItem).GetAwaiter().GetResult();

            log.LogInformation($"ServiceBus queue trigger function sent following message to blob: {queueItem}");
        }
        catch (Exception e)
        {
            log.LogError(e, "Function stopped its work with message: " + e.Message);
            throw;
        }
        finally
        {
            log.LogInformation("Function's work finished");
        }
    }

    private static async Task SendItemToBlob(string queueItem)
    {
        var container = new BlobStorage(_config);
        var name = $"order_{DateTime.Now:dd.MM.yyyy-hh:mm:ss}.json";

        using var stream = new MemoryStream(Encoding.Default.GetBytes(queueItem));
        await container.SaveAsync(stream, name);
    }
}
