// Copyright (c) Microsoft. All rights reserved.

using ClientApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<ClientApp.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection(nameof(AppSettings)));
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);

	if (!builder.HostEnvironment.BaseAddress.Contains("localhost"))
		return;

	var username = builder.Configuration.GetValue<string>("username");
	username = string.IsNullOrWhiteSpace(username) ? "local-developer" : username;
	client.DefaultRequestHeaders.Add("X-MS-CLIENT-PRINCIPAL-ID", username);
});
builder.Services.AddScoped<OpenAIPromptQueue>();
builder.Services.AddLocalStorageServices();
builder.Services.AddSessionStorageServices();
builder.Services.AddSpeechSynthesisServices();
builder.Services.AddSpeechRecognitionServices();
builder.Services.AddSingleton<ITextToSpeechPreferencesListener, TextToSpeechPreferencesListenerService>();
builder.Services.AddMudServices();
builder.Services.AddTransient<IPdfViewer, WebPdfViewer>();

await JSHost.ImportAsync(
    moduleName: nameof(JavaScriptModule),
    moduleUrl: $"../js/iframe.js?{Guid.NewGuid()}" /* cache bust */);

var host = builder.Build();
await host.RunAsync();
