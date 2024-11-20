using KEYLOOP.Entities.Orders;

namespace KEYLOOP.Entities.Parts;

public class PriceAvailability
{
    public string? PartId { get; set; }
    public string? BrandCode { get; set; }
    public int? PartCode { get; set; }
    public Price? ListPrice { get; set; }
    public Price? Surcharge { get; set; }
    public int? AvailableStock { get; set; }
    public bool? SalesBlocked { get; set; }
    public string? SalesBlockedDescription { get; set; }
    public bool? IsAvailableForBackOrder { get; set; }
    public int? LeadTimeInDays { get; set; }
    public string? MandatoryVehicleReferences { get; set; }
    public OrderPrices? OrderPrices { get; set; }
}