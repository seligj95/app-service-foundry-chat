# Azure App Service + Foundry Starter Template

A one-click Azure Developer CLI (azd) template that deploys Azure App Service with Foundry, complete with VNet integration, Application Insights, and a Blazor Server chat application for validation.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Virtual Network                         │
│  ┌──────────────────────┐    ┌──────────────────────────────┐   │
│  │  App Service Subnet  │    │  Private Endpoint Subnet     │   │
│  │     (10.0.1.0/26)    │    │       (10.0.2.0/27)          │   │
│  │                      │    │                              │   │
│  │  ┌────────────────┐  │    │  ┌────────────────────────┐  │   │
│  │  │  App Service   │──┼────┼──│  Private Endpoint      │  │   │ 
│  │  │    (P0v3)      │  │    │  │  (Foundry)             │  │   │
│  │  └────────────────┘  │    │  └───────────┬────────────┘  │   │
│  └──────────────────────┘    └──────────────┼───────────────┘   │
└─────────────────────────────────────────────┼───────────────────┘
                                              │
                              ┌───────────────▼───────────────┐
                              │           Foundry             │
                              │   (Cognitive Services/AI)     │
                              │     + GPT-4o Deployment       │
                              └───────────────────────────────┘
```

## Features

- **Azure App Service** (P0v3) with .NET 10 and VNet integration
- **Foundry** with private endpoint (no public access)
- **Managed Identity** authentication (keyless)
- **Application Insights** with auto-instrumentation and OpenTelemetry
- **Log Analytics** workspace with 30-day retention
- **Blazor Server** chat application with:
  - Multi-turn conversation history
  - Configurable system prompt
  - Token usage metrics per response
  - Connection health check
  - **In-app connection settings** to test different Foundry resources
  - Responsive UI with keyboard shortcuts

## Prerequisites

- [Azure Developer CLI (azd)](https://aka.ms/azd-install)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Azure subscription with permissions to create resources

## Quick Start

1. **Clone and initialize:**
   ```bash
   azd init -t seligj95/app-service-foundry-chat
   ```

2. **Deploy to Azure:**
   ```bash
   azd up
   ```

3. **Open the chat application:**
   ```bash
   azd show
   ```
   Click the Web endpoint URL to open the chat app and test your Foundry connection.

## Configuration

### In-App Connection Settings

The chat application includes a **Connection Settings** panel that allows you to change the Foundry endpoint and model deployment without redeploying.

**To access the settings:**
1. Click the **⚙️ gear icon** in the top-right corner of the chat header
2. Enter a different **Foundry Endpoint** URL and/or **Model Deployment** name
3. Click **Apply & Connect** to test the new configuration

**Use cases:**
- Test the same app against different Foundry resources
- Switch between model deployments (e.g., `gpt-4o` vs `gpt-4o-mini`)
- Connect to a Foundry resource in a different region
- Demo the app without deploying infrastructure (enter any accessible endpoint)

> **Note:** If no default endpoint is configured via app settings, the settings panel will appear automatically on first load.

### Environment Variables (Deployment Defaults)

| Variable | Description | Default |
|----------|-------------|---------|
| `MODEL_NAME` | AI model to deploy | `gpt-4o` |
| `MODEL_VERSION` | Model version | `2024-08-06` |
| `MODEL_CAPACITY` | Deployment capacity (TPM) | `10` |

These environment variables set the **default** connection. Users can override them at runtime using the in-app settings panel.

### Customization

To change the model or capacity:
```bash
azd env set MODEL_NAME gpt-4o-mini
azd env set MODEL_CAPACITY 20
azd up
```

### Chat Configuration

Edit `src/ChatApp/appsettings.json` to customize the chat behavior:

```json
{
  "Chat": {
    "SystemPrompt": "You are a helpful assistant. Keep responses concise and friendly.",
    "MaxConversationMessages": 20
  }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| `SystemPrompt` | Instructions given to the AI model | See above |
| `MaxConversationMessages` | Max messages kept in conversation history | `20` |

## Project Structure

```
├── azure.yaml              # AZD configuration
├── LICENSE                 # MIT License
├── infra/
│   ├── main.bicep          # Main orchestration
│   ├── main.parameters.json
│   ├── abbreviations.json
│   └── modules/
│       ├── network.bicep       # VNet + subnets
│       ├── monitoring.bicep    # Log Analytics + App Insights
│       ├── ai-foundry.bicep    # Foundry + PE + diagnostics
│       └── app-service.bicep   # App Service + VNet integration
└── src/
    └── ChatApp/            # Blazor Server application
        ├── Components/     # Razor components
        ├── Models/         # Data models
        ├── Services/       # Chat service with AI integration
        └── wwwroot/        # Static assets + JS interop
```

## Security

- **Private Networking**: Foundry is accessed via private endpoint only
- **Managed Identity**: No API keys stored; App Service authenticates via system-assigned identity
- **HTTPS Only**: App Service enforces HTTPS
- **TLS 1.2**: Minimum TLS version enforced

## Monitoring

Access monitoring data in the Azure Portal:
- **Application Insights**: Request traces, dependencies, exceptions
- **Log Analytics**: Foundry request/response logs, metrics
- **Diagnostic Settings**: Audit logs for all AI operations

## Cleanup

To delete all resources:
```bash
azd down
```

## License

MIT
