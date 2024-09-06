using Azure.AI.OpenAI;
using FunctionCall;
using FunctionCall.Agent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Sqlite;
using Microsoft.SemanticKernel.Memory;

#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0001


/*/// <summary>
/// The example shows how to use Bing and Google to search for current data
/// you might want to import into your system, e.g. providing AI prompts with
/// recent information, or for AI to generate recent information to display to users.
/// </summary>
public class BingAndGooglePlugins
{
    public async Task RunAsync()
    {


        if (openAIModelId is null || openAIApiKey is null)
        {
            Console.WriteLine("OpenAI credentials not found. Skipping example.");
            return;
        }

        Kernel kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: openAIModelId,
                apiKey: openAIApiKey)
            .Build();

        // Load Bing plugin
        string bingApiKey = TestConfiguration.Bing.ApiKey;
        if (bingApiKey is null)
        {
            Console.WriteLine("Bing credentials not found. Skipping example.");
        }
        else
        {
            var bingConnector = new BingConnector(bingApiKey);
            var bing = new WebSearchEnginePlugin(bingConnector);
            kernel.ImportPluginFromObject(bing, "bing");
            await Example1Async(kernel, "bing");
            await Example2Async(kernel);
        }
    }

    private async Task Example1Async(Kernel kernel, string searchPluginName)
    {
        Console.WriteLine("======== Bing and Google Search Plugins ========");

        // Run
        var question = "What's the largest building in the world?";
        var function = kernel.Plugins[searchPluginName]["search"];
        var result = await kernel.InvokeAsync(function, new() { ["query"] = question });

        Console.WriteLine(question);
        Console.WriteLine($"----{searchPluginName}----");
        Console.WriteLine(result.GetValue<string>());

        /* OUTPUT:

            What's the largest building in the world?
            ----
            The Aerium near Berlin, Germany is the largest uninterrupted volume in the world, while Boeing's
            factory in Everett, Washington, United States is the world's largest building by volume. The AvtoVAZ
            main assembly building in Tolyatti, Russia is the largest building in area footprint.
            ----
            The Aerium near Berlin, Germany is the largest uninterrupted volume in the world, while Boeing's
            factory in Everett, Washington, United States is the world's ...
       #1#
    }

    private async Task Example2Async(Kernel kernel)
    {
        Console.WriteLine("======== Use Search Plugin to answer user questions ========");

        const string SemanticFunction = """
                                        Answer questions only when you know the facts or the information is provided.
                                        When you don't have sufficient information you reply with a list of commands to find the information needed.
                                        When answering multiple questions, use a bullet point list.
                                        Note: make sure single and double quotes are escaped using a backslash char.

                                        [COMMANDS AVAILABLE]
                                        - bing.search

                                        [INFORMATION PROVIDED]
                                        {{ $externalInformation }}

                                        [EXAMPLE 1]
                                        Question: what's the biggest lake in Italy?
                                        Answer: Lake Garda, also known as Lago di Garda.

                                        [EXAMPLE 2]
                                        Question: what's the biggest lake in Italy? What's the smallest positive number?
                                        Answer:
                                        * Lake Garda, also known as Lago di Garda.
                                        * The smallest positive number is 1.

                                        [EXAMPLE 3]
                                        Question: what's Ferrari stock price? Who is the current number one female tennis player in the world?
                                        Answer:
                                        {{ '{{' }} bing.search "what\\'s Ferrari stock price?" {{ '}}' }}.
                                        {{ '{{' }} bing.search "Who is the current number one female tennis player in the world?" {{ '}}' }}.

                                        [END OF EXAMPLES]

                                        [TASK]
                                        Question: {{ $question }}.
                                        Answer:
                                        """;

        var question = "Who is the most followed person on TikTok right now? What's the exchange rate EUR:USD?";
        Console.WriteLine(question);

        var oracle = kernel.CreateFunctionFromPrompt(SemanticFunction,
            new OpenAIPromptExecutionSettings() { MaxTokens = 4096, Temperature = 0, TopP = 1 });

        var answer = await kernel.InvokeAsync(oracle, new KernelArguments()
        {
            ["question"] = question,
            ["externalInformation"] = string.Empty
        });

        var result = answer.GetValue<string>()!;

        // If the answer contains commands, execute them using the prompt renderer.
        if (result.Contains("bing.search", StringComparison.OrdinalIgnoreCase))
        {
            var promptTemplateFactory = new KernelPromptTemplateFactory();
            var promptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(result));

            Console.WriteLine("---- Fetching information from Bing...");
            var information = await promptTemplate.RenderAsync(kernel);

            Console.WriteLine("Information found:");
            Console.WriteLine(information);

            // Run the prompt function again, now including information from Bing
            answer = await kernel.InvokeAsync(oracle, new KernelArguments()
            {
                ["question"] = question,
                // The rendered prompt contains the information retrieved from search engines
                ["externalInformation"] = information
            });
        }
        else
        {
            Console.WriteLine("AI had all the information, no need to query Bing.");
        }

        Console.WriteLine("---- ANSWER:");
        Console.WriteLine(answer.GetValue<string>());
    }

}*/

var builder = Kernel.CreateBuilder();
builder.Services.AddLogging(c => c.SetMinimumLevel(LogLevel.Trace).AddConsole());

/*
builder.AddOpenAIChatCompletion("gpt-4o",
    new OpenAIClient(new Uri("http://127.0.0.1:14251"), new Azure.AzureKeyCredential("key")));
    */

builder.AddOpenAIChatCompletion("gpt-4o-mini",
    "sk-O8uZWKkEzVHa2jIG54F8269a27354c668f09A546444c0bCc", "", "", new HttpClient()
    {
        BaseAddress = new Uri("https://chatapi.nloli.xyz/v1/chat/completions")
    });

builder.Plugins.AddFromType<EmailPlugin>();
builder.Plugins.AddFromType<WeatherPlugin>();
builder.Plugins.AddFromType<BrainRegionPlugin>();

var kernel = builder.Build();

var memoryBuilder = new MemoryBuilder();
memoryBuilder.WithOpenAITextEmbeddingGeneration("zpoint", "api key", "", new HttpClient()
{
    BaseAddress = new Uri("http://127.0.0.1:14251/v1/embeddings")
});
IMemoryStore memoryStore = await SqliteMemoryStore.ConnectAsync("memstore.db");
memoryBuilder.WithMemoryStore(memoryStore);
var TextMemory = memoryBuilder.Build();

/*await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-04 8:12 今天天气：多云 节日：无");
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-04 8:12 [说话人(爱莉）]：舰长初次见面，我是爱莉希雅！[对话人(舰长）]");
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-04 8:12 [说话人(舰长）]：你好，在休伯利安感觉如何？ [对话人(爱莉）]");
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-04 8:12 [说话人(爱莉）]：非常不错呢！[对话人(舰长）]");

await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-05 13:12 今天天气：晴天 节日：无");
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-05 13:12 [说话人(爱莉）]：舰长你今天好像很苦恼呢 [对话人(舰长）]");
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-05 13:12 [说话人(舰长）]：啊啊啊，希儿好可爱，但是希儿有布洛妮娅，我好苦恼  [对话人(爱莉）]");
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-05 13:12 [说话人(爱莉）]：哎呀哎呀！ [对话人(舰长）]");

await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-06 20:12 今天天气：晴天 节日：中秋节");
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-06 20:12 [说话人(爱莉）]：舰长今天是晚上才来看我呢，事物繁忙吗？ [对话人(舰长）]");
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-06 20:12 [说话人(舰长）]：是啊，白天要工作，不是舰上的工作  [对话人(爱莉）]");
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-06 20:12 [说话人(爱莉）]：舰长好辛苦呢！ [对话人(舰长）]");*/

IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// 交互式测试工具

ChatHistory chatMessages = new ChatHistory();
while (true)
{
    System.Console.Write("User > ");
    string messageUser = Console.ReadLine()!;
    chatMessages.AddUserMessage(messageUser);

    var vectorSearch = await Rag.VectorSearch(TextMemory,"记忆", messageUser);

    string systemPrompt = "下面我们要进行角色扮演，你的名字叫{}，人物设定是：{人物设定},你正在对话的人的名字是{舰长} ，现在是{2024-09-06 20：46}，你之后回复的有关时间的文本都要以相对时刻说出，例如今天是9月15日，9月14日就要用昨天代替，" +
                          "获取到的历史相关信息如下，格式是类似于 2024-09-05 20:12 说话人（爱莉）：今天你吃了吗？对话人：（希儿）的格式，括号里的内容是名字，你需要根据人物设定判断是谁说的话：";
    foreach (var message in vectorSearch)
    {
        systemPrompt += message;
    }

    systemPrompt += "根据上述信息进行角色扮演，并在适当的时候调用工具，当没有显式说出调用工具的名称时不要去调用工具，调用工具的参数不能凭空捏造，在工具调用信息缺失时你会继续提问直到满足调用该工具的参数要求。";

    
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        ChatSystemPrompt = systemPrompt,
        Temperature = 0.0f
    };
    var result = chatCompletionService.GetChatMessageContentsAsync(
        chatMessages,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    string fullMessage = "";
    System.Console.Write("Assistant > ");
    Console.WriteLine(result.Result.FirstOrDefault()?.Content);
    System.Console.WriteLine();

    chatMessages.AddAssistantMessage(result.Result.FirstOrDefault()?.Content);
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