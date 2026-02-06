# Azure App Service + Azure AI Foundry Chat Starter

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/seligj95/app-service-foundry-chat)
[![Open in Dev Containers](https://img.shields.io/static/v1?style=for-the-badge&label=Dev%20Containers&message=Open&color=blue&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/seligj95/app-service-foundry-chat)

A one-click Azure Developer CLI (`azd`) template that deploys Azure App Service with Azure AI Foundry, complete with VNet integration, managed identity, Application Insights, and a Blazor Server chat application for validation.

Sample application code is included in this project. You can use or modify this app code or you can rip it out and include your own.

[Features](#features) • [Getting Started](#getting-started) • [Guidance](#guidance) • [Resources](#resources)

## Important Security Notice

This template, the application code and configuration it contains, has been built to showcase Microsoft Azure specific services and tools. We strongly advise our customers not to make this code part of their production environments without implementing or enabling additional security features. For a more comprehensive list of best practices and security recommendations for Intelligent Applications, visit our [official documentation](https://learn.microsoft.com/azure/security/fundamentals/overview).

## Features

This project framework provides the following features:

* **Azure App Service** (P0v3) with .NET 10 and VNet integration
* **Azure AI Foundry** with private endpoint (no public access)
* **Managed Identity** authentication (keyless, no API keys)
* **Application Insights** with auto-instrumentation and OpenTelemetry
* **Log Analytics** workspace with 30-day retention
* **Blazor Server** chat application with:
  * Multi-turn conversation history
  * Configurable system prompt
  * Token usage metrics per response
  * Connection health check
  * In-app connection settings to test different Foundry resources
  * Responsive UI with keyboard shortcuts

### Architecture Diagram

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
                              │       Azure AI Foundry        │
                              │   (Cognitive Services/AI)     │
                              │     + GPT-4o Deployment       │
                              └───────────────────────────────┘
```

## Getting Started

You have a few options for getting started with this template. The quickest way to get started is [GitHub Codespaces](#github-codespaces), since it will setup all the tools for you, but you can also [set it up locally](#local-environment). You can also use a [VS Code dev container](#vs-code-dev-containers).

This template uses GPT-4o which may not be available in all Azure regions. Check for [up-to-date region availability](https://learn.microsoft.com/azure/ai-services/openai/concepts/models#standard-deployment-model-availability) and select a region during deployment accordingly.

* We recommend using **East US 2** or **Sweden Central**

### GitHub Codespaces

You can run this template virtually by using GitHub Codespaces. The button will open a web-based VS Code instance in your browser:

1. Open the template (this may take several minutes):

    [![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/seligj95/app-service-foundry-chat)

2. Open a terminal window
3. Sign into your Azure account:

    ```shell
    azd auth login --use-device-code
    ```

4. Provision the Azure resources and deploy your code:

    ```shell
    azd up
    ```

5. Click the Web endpoint URL shown in the output to open the chat app.

### VS Code Dev Containers

A related option is VS Code Dev Containers, which will open the project in your local VS Code using the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers):

1. Start Docker Desktop (install it if not already installed)
2. Open the project:

    [![Open in Dev Containers](https://img.shields.io/static/v1?style=for-the-badge&label=Dev%20Containers&message=Open&color=blue&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/seligj95/app-service-foundry-chat)

3. In the VS Code window that opens, once the project files show up (this may take several minutes), open a terminal window.
4. Sign into your Azure account:

    ```shell
    azd auth login
    ```

5. Provision the Azure resources and deploy your code:

    ```shell
    azd up
    ```

6. Click the Web endpoint URL shown in the output to open the chat app.

### Local Environment

#### Prerequisites

* [Azure Developer CLI (azd)](https://aka.ms/install-azd)
  * Windows: `winget install microsoft.azd`
  * Linux: `curl -fsSL https://aka.ms/install-azd.sh | bash`
  * macOS: `brew tap azure/azd && brew install azd`
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* Azure subscription with permissions to create resources
* This template uses **GPT-4o** which may not be available in all Azure regions. Check for [up-to-date region availability](https://learn.microsoft.com/azure/ai-services/openai/concepts/models#standard-deployment-model-availability) and select a region during deployment accordingly.
  * We recommend using **East US 2** or **Sweden Central**

#### Quickstart

1. Bring down the template code:

    ```shell
    azd init -t seligj95/app-service-foundry-chat
    ```

    This will perform a git clone.

2. Sign into your Azure account:

    ```shell
    azd auth login
    ```

3. Provision and deploy the project to Azure:

    ```shell
    azd up
    ```

4. Click the Web endpoint URL shown in the output to open the chat app and test your Foundry connection.

5. Configure a CI/CD pipeline:

    ```shell
    azd pipeline config
    ```

#### Local Development

To run the app locally:

1. After running `azd up`, the app settings will be configured with the Foundry endpoint.
2. Navigate to the app directory:

    ```shell
    cd src/ChatApp
    ```

3. Run the app:

    ```shell
    dotnet run
    ```

4. Open `https://localhost:5001` in your browser.

> **Note:** Local development requires network access to the Azure AI Foundry endpoint. Since the Foundry resource uses a private endpoint, you may need to connect via the VNet or temporarily enable public access for local testing.

### Configuration

#### In-App Connection Settings

The chat application includes a **Connection Settings** panel that allows you to change the Foundry endpoint and model deployment without redeploying.

**To access the settings:**
1. Click the **⚙️ gear icon** in the top-right corner of the chat header.
2. Enter a different **Foundry Endpoint** URL and/or **Model Deployment** name.
3. Click **Apply & Connect** to test the new configuration.

**Use cases:**
- Test the same app against different Foundry resources
- Switch between model deployments (e.g., `gpt-4o` vs `gpt-4o-mini`)
- Connect to a Foundry resource in a different region

#### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `MODEL_NAME` | AI model to deploy | `gpt-4o` |
| `MODEL_VERSION` | Model version | `2024-08-06` |
| `MODEL_CAPACITY` | Deployment capacity (TPM) | `10` |

To change the model or capacity:

```shell
azd env set MODEL_NAME gpt-4o-mini
azd env set MODEL_CAPACITY 20
azd up
```

#### Chat Configuration

Edit `src/ChatApp/appsettings.json` to customize:

```json
{
  "Chat": {
    "SystemPrompt": "You are a helpful assistant. Keep responses concise and friendly.",
    "MaxConversationMessages": 20
  }
}
```

## Guidance

### Region Availability

This template uses **GPT-4o** which may not be available in all Azure regions. Check for [up-to-date region availability](https://learn.microsoft.com/azure/ai-services/openai/concepts/models#standard-deployment-model-availability) and select a region during deployment accordingly.

* We recommend using **East US 2** or **Sweden Central**

### Costs

You can estimate the cost of this project's architecture with [Azure's pricing calculator](https://azure.microsoft.com/pricing/calculator/).

* [Azure App Service](https://azure.microsoft.com/pricing/details/app-service/linux/) — P0v3 plan
* [Azure AI Services](https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/) — GPT-4o token-based pricing
* [Application Insights](https://azure.microsoft.com/pricing/details/monitor/) — Log ingestion
* [Virtual Network](https://azure.microsoft.com/pricing/details/virtual-network/) — Private endpoints

### Security

> [!NOTE]
> This template uses **Managed Identity** for authentication between App Service and Azure AI Foundry.

This template has [Managed Identity](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview) built in to eliminate the need for developers to manage credentials. Applications can use managed identities to obtain Microsoft Entra tokens without having to manage any credentials. Additionally, we have added a [GitHub Action tool](https://github.com/microsoft/security-devops-action) that scans the infrastructure-as-code files and generates a report containing any detected issues. To ensure best practices in your repo we recommend anyone creating solutions based on our templates ensure that the [GitHub secret scanning](https://docs.github.com/code-security/secret-scanning/about-secret-scanning) setting is enabled in your repos.

**Security features in this template:**
- **Private Networking**: Azure AI Foundry is accessed via private endpoint only
- **Managed Identity**: No API keys stored; App Service authenticates via system-assigned identity
- **HTTPS Only**: App Service enforces HTTPS
- **TLS 1.2**: Minimum TLS version enforced

## Resources

* [Azure App Service documentation](https://learn.microsoft.com/azure/app-service/)
* [Azure AI Foundry documentation](https://learn.microsoft.com/azure/ai-studio/)
* [Azure Developer CLI (azd) documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
* [Managed Identity overview](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview)
* [Azure OpenAI Service models](https://learn.microsoft.com/azure/ai-services/openai/concepts/models)
* [Develop .NET apps that use Azure AI services](https://learn.microsoft.com/dotnet/ai/)

### Project Structure

```
├── azure.yaml                  # AZD service configuration
├── infra/
│   ├── main.bicep              # Main orchestration
│   ├── main.parameters.json    # Parameters
│   ├── abbreviations.json      # Resource naming abbreviations
│   └── modules/
│       ├── network.bicep       # VNet + subnets
│       ├── monitoring.bicep    # Log Analytics + App Insights
│       ├── ai-foundry.bicep    # Foundry + PE + diagnostics
│       └── app-service.bicep   # App Service + VNet integration
└── src/
    └── ChatApp/                # Blazor Server application
        ├── Components/         # Razor components
        ├── Models/             # Data models
        ├── Services/           # Chat service with AI integration
        └── wwwroot/            # Static assets + JS interop
```

### Monitoring

Access monitoring data in the Azure Portal:
- **Application Insights**: Request traces, dependencies, exceptions
- **Log Analytics**: Foundry request/response logs, metrics
- **Diagnostic Settings**: Audit logs for all AI operations

### Cleanup

To delete all resources:

```shell
azd down
```
