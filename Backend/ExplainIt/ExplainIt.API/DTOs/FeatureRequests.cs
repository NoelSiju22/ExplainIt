namespace ExplainIt.API.DTOs;
public record IntelligenceRequest(string? SimpleSummary,string? WhatIsIt,string? WhyReceived,string? WhatToDo,string? Consequences,string? Status);
public record TagRequest(string Name);
public record ReminderRequest(string Title, DateTime RemindAt, Guid? DeadlineId, Guid? ActionId);
public record ShareRequest(DateTime? ExpiresAt);
public record QuestionRequest(string Question,string Mode);
public record VersionRequest(string? ChangeSummary);
