namespace ExplainIt.API.DTOs
{
    public class DocumentListResponse
    {
        public Guid Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsImportant { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string Status { get; set; } = "Needs attention";
    }
}