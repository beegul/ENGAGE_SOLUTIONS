namespace KEYLOOP.Entities.Orders;

public class Price
{
    public decimal? NetValue { get; set; }
    public decimal? GrossValue { get; set; }
    public decimal? TaxValue { get; set; }
    public int? TaxRate { get; set; }
    public string? CurrencyCode { get; set; }
}