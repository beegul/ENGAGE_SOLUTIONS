namespace KEYLOOP.Entities.Parts;

public class Part
{
    public string? PartId { get; set; }
    public int? PartCode { get; set; }
    public string? BrandCode { get; set; }
    public string? Description { get; set; }
    public AlternativeParts? AlternativeParts { get; set; }
    public decimal? Price { get; set; }
    public int? Availability { get; set; }
}