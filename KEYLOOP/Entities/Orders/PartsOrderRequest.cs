using System.ComponentModel.DataAnnotations;

namespace KEYLOOP.Entities.Orders;

public class PartsOrderRequest
{
    public string? CustomerId { get; set; }
    public string? CompanyId { get; set; }
    public OrderContact? OrderContact { get; set; }
    public AlternateDeliveryAddress? AlternateDeliveryAddress { get; set; }
    public string? OrderType { get; set; }
    public string? OrderReference { get; set; }
    
    [Required(ErrorMessage = "Parts list is required.")]
    [MinLength(1, ErrorMessage = "At least one part is required.")]
    public List<PartOrder>? Parts { get; set; }
}