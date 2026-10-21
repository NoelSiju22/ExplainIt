namespace ExplainIt.API.DTOs
{
    public class CreateDocumentRequest
    {
        public int CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? Description { get; set; }
        public bool IsImportant { get; set; } = false;
        public DateTime? DocumentDate { get; set; }
    }
}