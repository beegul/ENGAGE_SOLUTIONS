using KEYLOOP.Entities.Orders;

namespace KEYLOOP.Entities.Customer;

public class Relations
{
    public List<Customer>? Customers { get; set; }
    public List<Company>? Companies { get; set; }
}