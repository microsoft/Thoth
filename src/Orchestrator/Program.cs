using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using static System.Runtime.InteropServices.JavaScript.JSType;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var modelId = hostContext.Configuration["AOAI_MODEL_ID"] ?? throw new Exception("AOAI_MODEL_ID missing in configuration.");
        var endpoint = hostContext.Configuration["AOAI_ENDPOINT"] ?? throw new Exception("AOAI_ENDPOINT missing in configuration.");
        var pluginDir = hostContext.Configuration["AOAI_PLUGIN_DIR"] ?? throw new Exception("AOAI_PLUGIN_DIR missing in configuration.");
        var apiKey = hostContext.Configuration["AOAI_API_KEY"];

        var credential = new DefaultAzureCredential();

        var builder = string.IsNullOrWhiteSpace(apiKey)
            ? Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, credential)
            : Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

        var plugins = Directory.GetDirectories(pluginDir, "*", SearchOption.TopDirectoryOnly);

        foreach (var plugin in plugins)
            builder.Plugins.AddFromPromptDirectory(plugin);

        var kernel = builder.Build();

        services.AddSingleton(kernel);
    })
    .Build();

await host.RunAsync();
