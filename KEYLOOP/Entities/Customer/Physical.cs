namespace KEYLOOP.Entities.Customer;

public class Physical
{
    public string? StreetType { get; set; }
    public string? StreetName { get; set; }
    public string HouseNumber { get; set; } = null!;
    public string? BuildingName { get; set; }
    public string? FloorNumber { get; set; }
    public string? DoorNumber { get; set; }
    public string? BlockName { get; set; }
    public string? Estate { get; set; }
    public string? PostalCode { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public FormattedAddress? FormattedAddress { get; set; }
}