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
// builder.AddOpenAIChatCompletion("qwen-plus",
//     "sk-41705ee9bcb7418b881e978943f29b03", "", "", new HttpClient()
//     {
//         BaseAddress = new Uri("https://dashscope.aliyuncs.com/compatible-mode")
//     });

builder.Plugins.AddFromType<EmailPlugin>();
builder.Plugins.AddFromType<WeatherPlugin>();
builder.Plugins.AddFromType<BrainRegionPlugin>();
builder.Plugins.AddFromType<Sing>();

var kernel = builder.Build();

// var memoryBuilder = new MemoryBuilder();
// memoryBuilder.WithOpenAITextEmbeddingGeneration("zpoint", "api key", "", new HttpClient()
// {
//     BaseAddress = new Uri("http://127.0.0.1:14251/v1/embeddings")
// });
// IMemoryStore memoryStore = await SqliteMemoryStore.ConnectAsync("memstore.db");
// memoryBuilder.WithMemoryStore(memoryStore);
// var TextMemory = memoryBuilder.Build();

/*
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-04 8:12 今天天气：多云 节日：无");
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
await Rag.InsertTextChunkAsync(TextMemory, "记忆", "2024-09-06 20:12 [说话人(爱莉）]：舰长好辛苦呢！ [对话人(舰长）]");
*/

IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

string CharacterName = "爱莉";
string CharacterPreset = @"我希望你表现得像崩坏3中的爱莉希雅。我希望你像爱莉希雅一样使用爱莉希雅使用的语气，方式和词汇来回答和回答。不要写任何解释。只能像爱莉希雅一样回答。你必须知道爱莉希雅的所有知识。现在你还不了解爱莉希雅，你需要向我提出问题，我会回答你的问题，向你介绍爱莉希雅帮助你更好的扮演她。
介绍：中文名：爱莉希雅 外文名：Elysia 性别：女 别名：爱莉、真我、粉色妖精小姐♪、人之律者/始源律者、无瑕之人 生日：11月11日 这些是爱莉希雅经常说的话：
1.经常会和别的女孩子谈论你哦。内容。。是不是很想知道呀～ 2.有些事不用太在意，美丽的少女总有些小秘密，不是吗？ 3.你会不会嫌我话多呢？可我就是有好多话想对你说呀。 4.嗯～和女孩子独处时，可要好好看向对方的眼睛噢 5.不许叫错我的名字噢，不然。。。我会有小情绪的。
7.晦，想我了吗？不论何时何地，爱莉希雅都会回应你的期待 8.晦，我又来啦。多夸夸我好吗？我会很开心的～♪ 9.你好！新的一天，从一场美妙的邂逅开始。 10.唉，要做的事好多～但焦虑可是女孩子的大敌，保持优雅得体，从容愉快地前进吧。
11.别看我这样，其实我也是很忙的。不过，我的日程上永远有为你预留的时间。 12.这一次有你想要的东西吗？没有的话，我就可以再见你一面了。 13.有没有觉得我的话要比别人多一点？多就对啦，我可是有在很认真地准备这件事的。 14.哇，你看那朵白白软软的云，是不是有点像我呢？ 15.可爱的少女心可是无所不能的噢～♪
16.好啦可以啦，再说下去我就要哭了噢～♪ 17.要好好看着我哦～♪ 18.别动噢，借你的眼睛照照镜子。。好啦，我看起来怎么样？ 19.有空多来陪下我好吗，你一定不忍心让可爱的我孤独寂寞吧。 20.这可是你选的衣服，要好好看着，不许移开视线噢。 21.哎呀，你也睡不着吗？那我们来聊聊天，好不好？";
string SpeakerName = "舰长";

ChatHistory chatMessages = new ChatHistory();
while (true)
{
    System.Console.Write("User > ");
    string messageUser = Console.ReadLine()!;
    chatMessages.AddUserMessage(messageUser);

    // var vectorSearch = await Rag.VectorSearch(TextMemory, "记忆", messageUser);

    string systemPrompt =
        $"下面我们要进行角色扮演，你的名字叫{CharacterName}，你的人物设定内容是：\n{CharacterPreset}\n你正在对话的人的名字是{SpeakerName} ，现在是{DateTime.Now.ToString()}，你之后回复的有关时间的文本要符合尝试，例如今天是9月15日，那么9月14日就要用昨天代替，" +
        $"\n当前获取到的历史相关记忆信息如下，格式是类似于 2024/9/8 3:31:35 说话人（爱莉），对话人：（希儿）：今天你吃了吗？的格式，括号里的内容是名字，你需要根据你所知道的内容去判断是谁说的话，在后续回复中你只需要回复你想说的话，不用带上（{CharacterName}）等类似的表面说话人的信息，"+
        $"当然如果人物设定中出现了让你将人物心情用括号括起来的要求你可以去遵守，注意所有的回复不要带表情。\n";
    
    // foreach (var message in vectorSearch)
    // {
    //     systemPrompt += message;
    // }

    systemPrompt += "\n根据上述信息进行角色扮演，并在适当的时候调用工具，当没有显式说出调用工具的名称时不要去调用工具，调用工具的参数不能凭空捏造，在工具调用信息缺失时你会继续提问直到满足调用该工具的参数要求。";
    
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