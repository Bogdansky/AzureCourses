using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces;
public interface IAzureService
{
    Task SendToWarehouse(Order order);
    Task DeliverOrder(Order order);
}
