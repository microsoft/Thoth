// Copyright (c) Microsoft. All rights reserved.

using Azure.Core;
using Blazor.Serialization.Extensions;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;



namespace MinimalApi.Services;
#pragma warning disable SKEXP0011 // Mark members as static
#pragma warning disable SKEXP0001 // Mark members as static
public class ReadRetrieveReadChatService
{
    private readonly ISearchService _searchClient;
    private readonly Kernel _kernel;
    private readonly IConfiguration _configuration;
    private readonly TokenCredential? _tokenCredential;

    public ReadRetrieveReadChatService(
        ISearchService searchClient,
        OpenAIClient client,
        IConfiguration configuration,
        TokenCredential? tokenCredential = null)
    {
        _searchClient = searchClient;
        var kernelBuilder = Kernel.CreateBuilder();

        if (configuration["USE_AOAI"] == "false")
        {
            var deployment = configuration["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];
            ArgumentNullException.ThrowIfNullOrWhiteSpace(deployment);
            kernelBuilder = kernelBuilder.AddOpenAIChatCompletion(deployment, client);

            var embeddingModelName = configuration["AZURE_OPENAI_EMBEDDING_DEPLOYMENT"];
            ArgumentNullException.ThrowIfNullOrWhiteSpace(embeddingModelName);
            kernelBuilder = kernelBuilder.AddOpenAITextEmbeddingGeneration(embeddingModelName, client);
        }
        else
        {
            var deployedModelName = configuration["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];
            ArgumentNullException.ThrowIfNullOrWhiteSpace(deployedModelName);
            var embeddingModelName = configuration["AZURE_OPENAI_EMBEDDING_DEPLOYMENT"];
            if (!string.IsNullOrEmpty(embeddingModelName))
            {
                var endpoint = configuration["AZURE_OPENAI_ENDPOINT"];
                ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);
                kernelBuilder = kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(embeddingModelName, endpoint, tokenCredential ?? new DefaultAzureCredential());
                kernelBuilder = kernelBuilder.AddAzureOpenAIChatCompletion(deployedModelName, endpoint, tokenCredential ?? new DefaultAzureCredential());
            }
        }

        _kernel = kernelBuilder.Build();
        _configuration = configuration;
        _tokenCredential = tokenCredential;
    }

    public async Task<ChatAppResponse> ReplyAsync(
        ChatMessage[] history,
        RequestOverrides? overrides,
        CancellationToken cancellationToken = default)
    {
        var top = overrides?.Top ?? 3;
        var useSemanticCaptions = overrides?.SemanticCaptions ?? false;
        var useSemanticRanker = overrides?.SemanticRanker ?? false;
        var excludeCategory = overrides?.ExcludeCategory ?? null;
        var filter = excludeCategory is null ? null : $"category ne '{excludeCategory}'";
        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var embedding = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        float[]? embeddings = null;
        var question = history.LastOrDefault(m => m.IsUser)?.Content is { } userQuestion
            ? userQuestion
            : throw new InvalidOperationException("User question is null");

        string[]? followUpQuestionList = null;
        if (overrides?.RetrievalMode != RetrievalMode.Text && embedding is not null)
        {
            embeddings = (await embedding.GenerateEmbeddingAsync(question, cancellationToken: cancellationToken)).ToArray();
        }

        // step 1
        // use llm to get query if retrieval mode is not vector
        string? query = null;
        if (overrides?.RetrievalMode != RetrievalMode.Vector)
        {
            var getQueryChat = new ChatHistory(@"You are a helpful AI assistant, generate search query for followup question.
                                    Make your response simple and precise. Return the query only, do not return any other text.
                                    e.g.
                                    Northwind Health Plus AND standard plan.
                                    standard plan AND dental AND employee benefit.
                                    ");

            getQueryChat.AddUserMessage(question);
            var result = await chat.GetChatMessageContentAsync(
                getQueryChat,
                cancellationToken: cancellationToken);

            query = result.Content ?? throw new InvalidOperationException("Failed to get search query");
        }

        // step 2
        // use query to search related docs
        var documentContentList = await _searchClient.QueryDocumentsAsync(query, embeddings, overrides, cancellationToken);

        string documentContents = string.Empty;
        if (documentContentList.Length == 0)
        {
            documentContents = "no source available.";
        }
        else
        {
            documentContents = string.Join("\r", documentContentList.Select(x => $"{x.Title}:{x.Content}"));
        }

        // step 3
        // put together related docs and conversation history to generate answer
        var answerChat = new ChatHistory(
            "You are a system assistant who helps the company employees with their questions. Be brief in your answers");

        // add chat history
        foreach (var message in history)
        {
            if (message.IsUser)
            {
                answerChat.AddUserMessage(message.Content);
            }
            else
            {
                answerChat.AddAssistantMessage(message.Content);
            }
        }

        var prompt = $$$"""
            ## Source ##
            {{{documentContents}}}
            ## End ##
            Provide brief thoughts on how you came up with the answer, e.g. what sources you used, what you thought about, etc.
            If you can't answer a question with the information available in the source, please say you can't answer it. Do not use general knowledge.
            Your answer always will be in valid json whether you find relevant data or not.
            {
                "answer": "There is no information available on that subject.",
                "thoughts": "Did not find any relevant information."
            }
            ## Valid Answer Example: ##
            {
                "answer": "If the patient discloses after you have already started seeing them for other reasons and the note was already written.",
                "thoughts": "I found a similar case in document x and used that to extrapolate an answer."
            }
            ## End Example ##
            """;
        answerChat.AddUserMessage(prompt);

        var promptExecutingSetting = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1024,
            Temperature = overrides?.Temperature ?? 0.5,
            StopSequences = [],
        };

        // get answer
        var answer = await chat.GetChatMessageContentAsync(
                       answerChat,
                       promptExecutingSetting,
                       cancellationToken: cancellationToken);
        var answerJson = answer.Content ?? throw new InvalidOperationException("Failed to get search query");
        var answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
        var ans = answerObject.GetProperty("answer").GetString() ?? throw new InvalidOperationException("Failed to get answer");
        var thoughts = answerObject.GetProperty("thoughts").GetString() ?? throw new InvalidOperationException("Failed to get thoughts");

        int totalTokens = 0;
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };        


        if (answer.Metadata != null)
        {
            var usageDict = answer.Metadata["Usage"];
            if (usageDict != null)
            {
                string strJson = JsonSerializer.Serialize(usageDict, jsonOptions);

                JsonDocument doc = JsonDocument.Parse(strJson);
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("TotalTokens", out JsonElement totalTokensElement))
                {
                    totalTokens = totalTokensElement.GetInt32();
                }                
            }
        }

        // step 4
        // add follow up questions if requested
        if (overrides?.SuggestFollowupQuestions is true)
        {
            var followUpQuestionChat = new ChatHistory(@"You are a helpful AI assistant");
            followUpQuestionChat.AddUserMessage($@"Generate three follow-up question based on the answer you just generated.
                        # Answer
                        {ans}

                        # Format of the response
                        Return the follow-up question as a json string list. Don't put your answer between ```json and ```, return the json string directly.
                        e.g.
                        [
                            ""What is the deductible?"",
                            ""What is the co-pay?"",
                            ""What is the out-of-pocket maximum?""
                        ]");

            var followUpQuestions = await chat.GetChatMessageContentAsync(
                followUpQuestionChat,
                promptExecutingSetting,
                cancellationToken: cancellationToken);

            var followUpQuestionsJson = followUpQuestions.Content ?? throw new InvalidOperationException("Failed to get search query");
            var followUpQuestionsObject = JsonSerializer.Deserialize<JsonElement>(followUpQuestionsJson);
            var followUpQuestionsList = followUpQuestionsObject.EnumerateArray().Select(x => x.GetString()!).ToList();
            foreach (var followUpQuestion in followUpQuestionsList)
            {
                ans += $" <<{followUpQuestion}>> ";
            }

            followUpQuestionList = followUpQuestionsList.ToArray();
        }

        var responseMessage = new ResponseMessage("assistant", ans, totalTokens);
        var responseContext = new ResponseContext(
            DataPointsContent: documentContentList.Select(x => new SupportingContentRecord(x.Title, x.Content)).ToArray(),
            FollowupQuestions: followUpQuestionList ?? Array.Empty<string>(),
            Thoughts: [new Thoughts("Thoughts", thoughts)]);

        var choice = new ResponseChoice(
            Index: 0,
            Message: responseMessage,
            Context: responseContext,
            CitationBaseUrl: _configuration.ToCitationBaseUrl());

        return new ChatAppResponse([choice]);
    }
}
