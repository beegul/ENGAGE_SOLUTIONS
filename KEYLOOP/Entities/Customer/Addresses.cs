namespace KEYLOOP.Entities.Customer;

public class Addresses
{
    public Physical Physical { get; set; } = new();
    public Postal Postal { get; set; } = new();
}