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

public record ConnectionConfig(
    string Endpoint,
    string ModelDeployment
);

public class ChatSettings
{
    public string SystemPrompt { get; set; } = "You are a helpful assistant. Keep responses concise and friendly.";
    public int MaxConversationMessages { get; set; } = 20;
}

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(string conversationId, string userMessage, ConnectionConfig? config = null, CancellationToken cancellationToken = default);
    Task<long> PingAsync(ConnectionConfig? config = null, CancellationToken cancellationToken = default);
    ServiceInfo GetServiceInfo();
    ConnectionConfig? GetDefaultConfig();
    void ClearConversation(string conversationId);
}

public class ChatService : IChatService
{
    private readonly ILogger<ChatService> _logger;
    private readonly DefaultAzureCredential _credential;
    private readonly ChatSettings _settings;
    private readonly string? _defaultEndpoint;
    private readonly string? _defaultModelDeployment;
    private readonly Dictionary<string, List<ChatMessage>> _conversations = new();
    private readonly Dictionary<string, ChatClient> _clientCache = new();
    private readonly object _lock = new();

    public ChatService(ILogger<ChatService> logger, DefaultAzureCredential credential, IOptions<ChatSettings> settings)
    {
        _logger = logger;
        _credential = credential;
        _settings = settings.Value;
        _defaultEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT");
        _defaultModelDeployment = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT");
        
        _logger.LogInformation("ChatService initialized. Default endpoint configured: {HasEndpoint}", !string.IsNullOrEmpty(_defaultEndpoint));
    }

    public ConnectionConfig? GetDefaultConfig()
    {
        if (string.IsNullOrEmpty(_defaultEndpoint))
            return null;
            
        return new ConnectionConfig(
            _defaultEndpoint,
            _defaultModelDeployment ?? "gpt-4o"
        );
    }

    public ServiceInfo GetServiceInfo()
    {
        var defaultConfig = GetDefaultConfig();
        return new ServiceInfo(
            defaultConfig?.Endpoint ?? "Not configured",
            defaultConfig?.ModelDeployment ?? "gpt-4o",
            defaultConfig != null,
            defaultConfig == null ? "No default endpoint configured. Enter connection details below." : null
        );
    }

    private ChatClient GetOrCreateClient(ConnectionConfig config)
    {
        var cacheKey = $"{config.Endpoint}|{config.ModelDeployment}";
        
        lock (_lock)
        {
            if (_clientCache.TryGetValue(cacheKey, out var cachedClient))
            {
                return cachedClient;
            }

            var azureClient = new AzureOpenAIClient(new Uri(config.Endpoint), _credential);
            var chatClient = azureClient.GetChatClient(config.ModelDeployment);
            _clientCache[cacheKey] = chatClient;
            
            _logger.LogInformation("Created new ChatClient for endpoint: {Endpoint}, model: {Model}", 
                config.Endpoint, config.ModelDeployment);
            
            return chatClient;
        }
    }

    public void ClearConversation(string conversationId)
    {
        lock (_lock)
        {
            _conversations.Remove(conversationId);
        }
        _logger.LogInformation("Cleared conversation: {ConversationId}", conversationId);
    }

    public async Task<long> PingAsync(ConnectionConfig? config = null, CancellationToken cancellationToken = default)
    {
        var effectiveConfig = config ?? GetDefaultConfig();
        if (effectiveConfig == null)
        {
            throw new InvalidOperationException("No connection configuration provided and no default configured.");
        }

        var chatClient = GetOrCreateClient(effectiveConfig);
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

            await chatClient.CompleteChatAsync(messages, options, cancellationToken);
            sw.Stop();
            
            _logger.LogInformation("Ping successful. Endpoint: {Endpoint}, Latency: {Latency}ms", 
                effectiveConfig.Endpoint, sw.ElapsedMilliseconds);
            return sw.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping failed for endpoint: {Endpoint}", effectiveConfig.Endpoint);
            throw;
        }
    }

    public async Task<ChatResponse> SendMessageAsync(string conversationId, string userMessage, ConnectionConfig? config = null, CancellationToken cancellationToken = default)
    {
        var effectiveConfig = config ?? GetDefaultConfig();
        if (effectiveConfig == null)
        {
            throw new InvalidOperationException("No connection configuration provided and no default configured.");
        }

        var chatClient = GetOrCreateClient(effectiveConfig);
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

            var response = await chatClient.CompleteChatAsync(messagesToSend, cancellationToken: cancellationToken);
            sw.Stop();

            var content = response.Value.Content.FirstOrDefault()?.Text ?? "No response received.";
            var usage = response.Value.Usage;

            // Add assistant response to history
            lock (_lock)
            {
                conversationHistory.Add(new AssistantChatMessage(content));
            }
            
            _logger.LogInformation(
                "Chat completion successful. ConversationId: {ConversationId}, Endpoint: {Endpoint}, Tokens: {Prompt}/{Completion}/{Total}, Latency: {Latency}ms", 
                conversationId,
                effectiveConfig.Endpoint,
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
                Model: effectiveConfig.ModelDeployment
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat completion for conversation: {ConversationId}, endpoint: {Endpoint}", 
                conversationId, effectiveConfig.Endpoint);
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
