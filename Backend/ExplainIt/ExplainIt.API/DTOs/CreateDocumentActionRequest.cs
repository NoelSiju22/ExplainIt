namespace ExplainIt.API.DTOs
{
    public class CreateDocumentActionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? Deadline { get; set; }
    }
}