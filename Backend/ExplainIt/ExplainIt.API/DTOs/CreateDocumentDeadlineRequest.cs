namespace ExplainIt.API.DTOs
{
    public class CreateDocumentDeadlineRequest
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public bool IsCompleted { get; set; } = false;
    }
}