using KEYLOOP.Entities.Parts;

namespace KEYLOOP.Entities.Orders;

public class PartOrder
{
    public Part? Part { get; set; }
    public int? Quantity { get; set; }
    public List<MandatoryVehicleReference>? MandatoryVehicleReferences { get; set; }
}