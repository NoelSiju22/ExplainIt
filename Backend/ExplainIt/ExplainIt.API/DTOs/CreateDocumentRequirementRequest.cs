namespace ExplainIt.API.DTOs
{
    public class CreateDocumentRequirementRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Completed { get; set; } = false;
    }
}