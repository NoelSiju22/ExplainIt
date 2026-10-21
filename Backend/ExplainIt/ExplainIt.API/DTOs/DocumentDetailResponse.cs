namespace ExplainIt.API.DTOs
{
    public class DocumentDetailResponse
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
        public string? SimpleSummary { get; set; }
        public string? WhatIsIt { get; set; }
        public string? WhyReceived { get; set; }
        public string? WhatToDo { get; set; }
        public string? Consequences { get; set; }
        public DateTime? LastViewedAt { get; set; }

        public List<DocumentActionResponse> Actions { get; set; } = new();
        public List<DocumentDeadlineResponse> Deadlines { get; set; } = new();
        public List<DocumentRequirementResponse> Requirements { get; set; } = new();
        public List<DocumentTermResponse> Terms { get; set; } = new();
        public List<DocumentNoteResponse> Notes { get; set; } = new();
        public List<DocumentVersionResponse> Versions { get; set; } = new();
        public List<DocumentTagResponse> Tags { get; set; } = new();
        public List<DocumentEventResponse> Events { get; set; } = new();
        public List<DocumentReminderResponse> Reminders { get; set; } = new();
        public List<DocumentQuestionResponse> Questions { get; set; } = new();
    }

    public class DocumentActionResponse
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
    }

    public class DocumentDeadlineResponse
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class DocumentRequirementResponse
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Completed { get; set; }
    }

    public class DocumentTermResponse
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Term { get; set; } = string.Empty;
        public string SimpleExplanation { get; set; } = string.Empty;
    }

    public class DocumentNoteResponse
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class DocumentVersionResponse { public Guid Id { get; set; } public Guid DocumentId { get; set; } public int VersionNumber { get; set; } public string Title { get; set; } = string.Empty; public string? Description { get; set; } public string FilePath { get; set; } = string.Empty; public string? ChangeSummary { get; set; } public DateTime CreatedAt { get; set; } }
public class DocumentTagResponse { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
public class DocumentEventResponse { public Guid Id { get; set; } public string Type { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public DateTime CreatedAt { get; set; } }
public class DocumentReminderResponse { public Guid Id { get; set; } public string Title { get; set; } = string.Empty; public DateTime RemindAt { get; set; } public bool IsRead { get; set; } public Guid DocumentId { get; set; } }
public class DocumentQuestionResponse { public Guid Id { get; set; } public string Question { get; set; } = string.Empty; public string Answer { get; set; } = string.Empty; public string Mode { get; set; } = "simple"; public DateTime CreatedAt { get; set; } }

}
