using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Diagnostics;

namespace ChatApp.Services;

public record ChatResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    long ResponseTimeMs,
    string Model
);

public record ServiceInfo(
    string Endpoint,
    string ModelDeployment,
    bool IsConfigured,
    string? ConfigurationError
);

public class ChatSettings
{
    public string SystemPrompt { get; set; } = "You are a helpful assistant. Keep responses concise and friendly.";
    public int MaxConversationMessages { get; set; } = 20;
}

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(string conversationId, string userMessage, CancellationToken cancellationToken = default);
    Task<long> PingAsync(CancellationToken cancellationToken = default);
    ServiceInfo GetServiceInfo();
    void ClearConversation(string conversationId);
    bool IsConfigured { get; }
    string? ConfigurationError { get; }
}

public class ChatService : IChatService
{
    private readonly ChatClient? _chatClient;
    private readonly ILogger<ChatService> _logger;
    private readonly string? _configurationError;
    private readonly string _endpoint;
    private readonly string _modelDeployment;
    private readonly ChatSettings _settings;
    private readonly Dictionary<string, List<ChatMessage>> _conversations = new();
    private readonly object _lock = new();

    public bool IsConfigured => _chatClient != null;
    public string? ConfigurationError => _configurationError;

    public ChatService(ILogger<ChatService> logger, DefaultAzureCredential credential, IOptions<ChatSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _endpoint = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT") ?? "";
        _modelDeployment = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT") ?? "gpt-4o";

        if (string.IsNullOrEmpty(_endpoint))
        {
            _configurationError = "AZURE_AI_FOUNDRY_ENDPOINT environment variable is not set.";
            _logger.LogError("Configuration error: {Error}", _configurationError);
            return;
        }

        try
        {
            var azureClient = new AzureOpenAIClient(new Uri(_endpoint), credential);
            _chatClient = azureClient.GetChatClient(_modelDeployment);
            _logger.LogInformation("ChatService initialized successfully with endpoint: {Endpoint}", _endpoint);
        }
        catch (Exception ex)
        {
            _configurationError = $"Failed to initialize AI client: {ex.Message}";
            _logger.LogError(ex, "Failed to initialize ChatService");
        }
    }

    public ServiceInfo GetServiceInfo()
    {
        return new ServiceInfo(
            string.IsNullOrEmpty(_endpoint) ? "Not configured" : _endpoint, 
            _modelDeployment, 
            IsConfigured, 
            _configurationError);
    }

    public void ClearConversation(string conversationId)
    {
        lock (_lock)
        {
            _conversations.Remove(conversationId);
        }
        _logger.LogInformation("Cleared conversation: {ConversationId}", conversationId);
    }

    public async Task<long> PingAsync(CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            throw new InvalidOperationException(_configurationError ?? "Chat client is not configured.");
        }

        var sw = Stopwatch.StartNew();
        
        try
        {
            var messages = new List<ChatMessage>
            {
                new UserChatMessage("hi")
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1
            };

            await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            sw.Stop();
            
            _logger.LogInformation("Ping successful. Latency: {Latency}ms", sw.ElapsedMilliseconds);
            return sw.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping failed");
            throw;
        }
    }

    public async Task<ChatResponse> SendMessageAsync(string conversationId, string userMessage, CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            throw new InvalidOperationException(_configurationError ?? "Chat client is not configured.");
        }

        var sw = Stopwatch.StartNew();

        try
        {
            List<ChatMessage> conversationHistory;
            lock (_lock)
            {
                if (!_conversations.TryGetValue(conversationId, out conversationHistory!))
                {
                    conversationHistory = new List<ChatMessage>
                    {
                        new SystemChatMessage(_settings.SystemPrompt)
                    };
                    _conversations[conversationId] = conversationHistory;
                }
            }

            // Add user message to history
            var userChatMessage = new UserChatMessage(userMessage);
            lock (_lock)
            {
                conversationHistory.Add(userChatMessage);
            }

            // Prepare messages for API call (with truncation if needed)
            List<ChatMessage> messagesToSend;
            lock (_lock)
            {
                messagesToSend = TruncateConversation(conversationHistory);
            }

            var response = await _chatClient.CompleteChatAsync(messagesToSend, cancellationToken: cancellationToken);
            sw.Stop();

            var content = response.Value.Content.FirstOrDefault()?.Text ?? "No response received.";
            var usage = response.Value.Usage;

            // Add assistant response to history
            lock (_lock)
            {
                conversationHistory.Add(new AssistantChatMessage(content));
            }
            
            _logger.LogInformation(
                "Chat completion successful. ConversationId: {ConversationId}, Tokens: {Prompt}/{Completion}/{Total}, Latency: {Latency}ms", 
                conversationId,
                usage?.InputTokenCount, 
                usage?.OutputTokenCount, 
                usage?.TotalTokenCount,
                sw.ElapsedMilliseconds);
            
            return new ChatResponse(
                Content: content,
                PromptTokens: usage?.InputTokenCount ?? 0,
                CompletionTokens: usage?.OutputTokenCount ?? 0,
                TotalTokens: usage?.TotalTokenCount ?? 0,
                ResponseTimeMs: sw.ElapsedMilliseconds,
                Model: _modelDeployment
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat completion for conversation: {ConversationId}", conversationId);
            throw;
        }
    }

    private List<ChatMessage> TruncateConversation(List<ChatMessage> history)
    {
        // Always keep the system message (first) and recent messages
        if (history.Count <= _settings.MaxConversationMessages + 1)
        {
            return new List<ChatMessage>(history);
        }

        var result = new List<ChatMessage> { history[0] }; // System message
        var recentMessages = history.Skip(history.Count - _settings.MaxConversationMessages).ToList();
        result.AddRange(recentMessages);
        
        return result;
    }
}
