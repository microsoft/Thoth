using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Orchestrator;

public static partial class Orchestrator
{
    [Function(nameof(RunOrchestrator))]
    public static async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestrator));
        logger.LogInformation("Saying hello.");

        var input = context.GetInput<string>();

        var outputs = new List<string>();

        outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), input));

        return outputs;
    }

    [Function(nameof(SayHello))]
    public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("SayHello");
        logger.LogInformation("Saying hello to {name}.", name);
        return $"Hello {name}!";
    }
}
