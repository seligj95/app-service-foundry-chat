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
                              │           Foundry            │
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
- **Blazor Server** chat application for testing connectivity

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

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `MODEL_NAME` | AI model to deploy | `gpt-4o` |
| `MODEL_VERSION` | Model version | `2024-08-06` |
| `MODEL_CAPACITY` | Deployment capacity (TPM) | `10` |

### Customization

To change the model or capacity:
```bash
azd env set MODEL_NAME gpt-4o-mini
azd env set MODEL_CAPACITY 20
azd up
```

## Project Structure

```
├── azure.yaml              # AZD configuration
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
        ├── Components/
        ├── Services/
        └── wwwroot/
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
