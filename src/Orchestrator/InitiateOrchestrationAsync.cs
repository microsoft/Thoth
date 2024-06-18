using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Net;

namespace Orchestrator;

public partial class Orchestrator
{
    [Function(nameof(InitiateOrchestrationAsync))]
    public static async Task<HttpResponseData> InitiateOrchestrationAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(InitiateOrchestrationAsync));

        try
        {
            var queryInput = req.Query["subject"];

            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(RunOrchestrator), queryInput);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }

    }
}
