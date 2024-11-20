namespace KEYLOOP.Entities.Customer;

public class Customer
{
    public string? CustomerId { get; set; }
    public string? Reference { get; set; }
    public string? Status { get; set; }
    public string? LanguageCode { get; set; }
    public Individual? Individual { get; set; }
    public Addresses Addresses { get; set; } = new();
    public Communications? Communications { get; set; }
    public AdditionalDetail? AdditionalDetail { get; set; }
    public Business? Business { get; set; }
    public List<Vehicle>? Vehicles { get; set; }
    public Relations? Relations { get; set; }
    public List<Branch>? Branches { get; set; }
    public UpdateHistory? UpdateHistory { get; set; }
}