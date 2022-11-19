using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DeliveryOrderProcessorInfrastucture.Models;

public record OrderCosmos
{
    // for identifying within Cosmos DB
    public string id { get; set; }
    public int OrderId { get; set; }
    public Address? ShipToAddress { get; set; }
    public List<OrderItem>? OrderItems { get; set; }
    public decimal TotalPrice { get; set; }
}
