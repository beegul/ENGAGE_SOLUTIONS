namespace KEYLOOP.Entities.Parts;

public class AlternativeParts
{
    public DateTime? SupersessionDate { get; set; }
    public string? AlternativeType { get; set; }
    public List<Part>? Parts { get; set; }
}