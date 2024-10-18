using System;
using System.Linq;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = Kernel.CreateBuilder();
builder.Services.AddLogging(c => c.SetMinimumLevel(LogLevel.Trace).AddConsole());

builder.AddOpenAIChatCompletion("default",
    "123456", "", "", new HttpClient()
    {
        BaseAddress = new Uri("http://127.0.0.1:14251/v1"),
        Timeout = TimeSpan.FromMinutes(3)
    });
var kernel = builder.Build();


IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var result = await chatCompletionService.GetChatMessageContentsAsync("hello world");
Console.WriteLine(result.FirstOrDefault()?.Content);