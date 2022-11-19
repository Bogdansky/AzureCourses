using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryOrderProcessorInfrastucture.Models
{
    public class Order
    {
        public int Id { get; set; }
        public Address ShipToAddress { get; set; }
        public List<OrderItem> OrderItems { get; set; }
    }
}
