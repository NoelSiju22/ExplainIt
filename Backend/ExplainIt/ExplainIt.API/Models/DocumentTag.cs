namespace ExplainIt.API.Models;
public class DocumentTag
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Document? Document { get; set; }
}
