namespace KEYLOOP.Entities.Parts;

public class PartResponse
{
    public List<Part>? Parts { get; set; }
    public int? TotalItems { get; set; }
    public int? TotalPages { get; set; }
    public Links? Links { get; set; }
}