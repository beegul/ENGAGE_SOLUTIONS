namespace KEYLOOP.Entities.Brands;

public class BrandResponse
{
    public List<Brand> Brands { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}