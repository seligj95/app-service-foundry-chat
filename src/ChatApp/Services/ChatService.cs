using Azure.AI.OpenAI;
using Azure.Identity;
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

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(string userMessage, CancellationToken cancellationToken = default);
    Task<long> PingAsync(CancellationToken cancellationToken = default);
    ServiceInfo GetServiceInfo();
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

    public bool IsConfigured => _chatClient != null;
    public string? ConfigurationError => _configurationError;

    public ChatService(ILogger<ChatService> logger, DefaultAzureCredential credential)
    {
        _logger = logger;
        _endpoint = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT") ?? "";
        _modelDeployment = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT") ?? "gpt-4o";

        if (string.IsNullOrEmpty(_endpoint))
        {
            _configurationError = "AZURE_AI_FOUNDRY_ENDPOINT environment variable is not set.";
            _logger.LogError(_configurationError);
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

    public async Task<ChatResponse> SendMessageAsync(string userMessage, CancellationToken cancellationToken = default)
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
                new SystemChatMessage("You are a helpful assistant. Keep responses concise and friendly."),
                new UserChatMessage(userMessage)
            };

            var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            sw.Stop();

            var content = response.Value.Content.FirstOrDefault()?.Text ?? "No response received.";
            var usage = response.Value.Usage;
            
            _logger.LogInformation(
                "Chat completion successful. Tokens: {Prompt}/{Completion}/{Total}, Latency: {Latency}ms", 
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
            _logger.LogError(ex, "Error during chat completion");
            throw;
        }
    }
}
