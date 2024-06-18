using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Runtime.CompilerServices;

namespace Orchestrator;

public partial class Orchestrator
{
    private readonly Kernel _kernel;

    public Orchestrator(Kernel kernel)
    {
        _kernel = kernel;
    }

    [Function(nameof(RunOrchestrator))]
    public static async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestrator));

        logger.LogInformation("Writing a poem.");

        var input = context.GetInput<string>();

        var outputs = new List<string>();

        outputs.Add(await context.CallActivityAsync<string>(nameof(WritePoemAsync), input));

        return outputs;
    }

    [Function(nameof(WritePoemAsync))]
    public async Task<string> WritePoemAsync([ActivityTrigger] string subject, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(WritePoemAsync));

        try
        {
            var skargs = new KernelArguments
            {
                { "input", subject }
            };

            var result = await _kernel.InvokeAsync("WriterPlugin", "ShortPoem", skargs);

            logger.LogInformation($"Generated poem about {subject}.");

            return result.ToString() ?? "No poem generated.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred.");
        }

        return "No poem generated.";
    }
}
