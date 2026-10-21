namespace ExplainIt.API.DTOs
{
    public class CreateDocumentTermRequest
    {
        public string Term { get; set; } = string.Empty;
        public string SimpleExplanation { get; set; } = string.Empty;
    }
}