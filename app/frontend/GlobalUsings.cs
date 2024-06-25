// Copyright (c) Microsoft. All rights reserved.

global using System.Collections.Concurrent;
global using System.Runtime.InteropServices.JavaScript;
global using ClientApp.Components;
global using ClientApp.Interop;
global using ClientApp.Options;
global using ClientApp.Services;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Forms;
global using Microsoft.AspNetCore.Components.Routing;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
global using Microsoft.Extensions.DependencyInjection;
global using MudBlazor;
global using MudBlazor.Services;

global using System.Globalization;
global using System.Net.Http.Json;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using ClientApp.Models;
global using Markdig;
global using Microsoft.Extensions.Logging;
global using Microsoft.JSInterop;
global using Shared.Json;
global using Shared.Models;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ClientApp.Tests")]
