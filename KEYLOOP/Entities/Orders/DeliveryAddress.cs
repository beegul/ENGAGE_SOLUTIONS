namespace KEYLOOP.Entities.Orders;

public class DeliveryAddress
{
    public string? StreetName { get; set; }
    public int? PostalCode { get; set; }
    public string? City { get; set; }
    public string? CountryCode { get; set; }
}