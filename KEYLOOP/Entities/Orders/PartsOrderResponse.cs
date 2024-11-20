namespace KEYLOOP.Entities.Orders;

public class PartsOrderResponse
{
    public int? PartsOrderId { get; set; }
    public DateTime? PartsOrderDateTime { get; set; }
    public string? OrderStatus { get; set; }
    public Customer? Customer { get; set; }
    public Company? Company { get; set; }
    public OrderContact? OrderContact { get; set; }
    public DeliveryAddress? DeliveryAddress { get; set; }
    public string? OrderType { get; set; }
    public string? OrderReference { get; set; }
    public PartsOrder? Parts { get; set; }
}