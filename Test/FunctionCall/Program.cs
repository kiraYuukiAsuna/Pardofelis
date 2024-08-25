using System;
using System.Linq;
using Azure.AI.OpenAI;
using FunctionCall.Agent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = Kernel.CreateBuilder();
builder.Services.AddLogging(c => c.SetMinimumLevel(LogLevel.Trace).AddConsole());

builder.AddOpenAIChatCompletion("gpt-4o", new OpenAIClient(new Uri("http://127.0.0.1:9090"), new Azure.AzureKeyCredential("sk-O8uZWKkEzVHa2jIG54F8269a27354c668f09A546444c0bCc")));

builder.Plugins.AddFromType<EmailPlugin>();
builder.Plugins.AddFromType<WeatherPlugin>();

var kernel = builder.Build();


IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// 交互式测试工具

ChatHistory chatMessages = new ChatHistory();
while (true)
{
    System.Console.Write("User > ");
    chatMessages.AddUserMessage(Console.ReadLine()!);

    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    };
    var result = chatCompletionService.GetChatMessageContentsAsync(
        chatMessages,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    string fullMessage = "";
    System.Console.Write("Assistant > ");
    Console.WriteLine(result.Result.FirstOrDefault()?.Content);
    System.Console.WriteLine();

    chatMessages.AddAssistantMessage(fullMessage);
}

/*var client = new ChatClient(model: "gpt-4o",
    "sk-O8uZWKkEzVHa2jIG54F8269a27354c668f09A546444c0bCc",
    options: new OpenAIClientOptions()
    {
        Endpoint = new Uri("https://chatapi.nloli.xyz/v1"),
        NetworkTimeout = TimeSpan.FromSeconds(10),
        RetryPolicy = new ClientRetryPolicy(3)
    });
ChatCompletion completion = client.CompleteChat(chatHistory.ChatHistory);
foreach (var c in completion.Content)
{
    var output = c.Text;
    _logger.Debug("Message: {output}", output);

    result.Append(output);
    completion_tokens += c.Text.Length;
}*/

