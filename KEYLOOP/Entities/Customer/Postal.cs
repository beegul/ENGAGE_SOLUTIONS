namespace KEYLOOP.Entities.Customer;

public class Postal
{
    public string? PoBoxName { get; set; }
    public string PoBoxNumber { get; set; } = null!;
    public string? PoBoxSuite { get; set; }
    public string? PostalCode { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public FormattedAddress? FormattedAddress { get; set; }
}