namespace KEYLOOP.Entities.Orders;

public class PartsOrder
{
    public string? OrderLineId { get; set; }
    public string? PartId { get; set; }
    public int? Quantity { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public int? UnitOfSale { get; set; }
    public string? PartsOrderLineStatus { get; set; }
    public MandatoryVehicleReference? MandatoryVehicleReference { get; set; }
    public Price? ListPrice { get; set; }
    public Price? OrderPrice { get; set; }
}