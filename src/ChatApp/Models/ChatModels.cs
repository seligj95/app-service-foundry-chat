namespace ChatApp.Models;

public record MessageMetrics(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    long ResponseTimeMs
);

public record ChatMessageViewModel(
    string Role,
    string Content,
    DateTime Timestamp,
    MessageMetrics? Metrics
);
