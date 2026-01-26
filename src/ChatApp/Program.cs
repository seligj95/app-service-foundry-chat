using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using ChatApp.Components;
using ChatApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry with Azure Monitor
builder.Services.AddOpenTelemetry().UseAzureMonitor();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add chat service
builder.Services.AddSingleton<IChatService, ChatService>();

// Add Azure credential for managed identity
builder.Services.AddSingleton(new DefaultAzureCredential());

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
