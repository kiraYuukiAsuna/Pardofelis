using PardofelisCore;
using PardofelisCore.Config;
using PardofelisCore.Util;
using PardofelisCore.VoiceOutput;
using PardofelisCore.VoiceInput;
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

Thread thread = new Thread(() =>
{
    InvokeMethod.Run();
});
thread.Start();

CancellationTokenSource cancellationToken = new CancellationTokenSource();

PythonInstance pythonInstance = new PythonInstance(CommonConfig.PythonRootPath);

var code = File.ReadAllText(Path.Join(CommonConfig.PardofelisAppDataPath, @"VoiceModel\VoiceOutput\infer.py"));

var scripts = new List<string>();
scripts.Add(code);

pythonInstance.StartPythonEngine(cancellationToken, scripts);

var builder = Kernel.CreateBuilder();
builder.Services.AddLogging(c => c.SetMinimumLevel(LogLevel.Trace).AddConsole());

var apiConfig = ModelParameterConfig.ReadConfig(Path.Join(CommonConfig.PardofelisAppDataPath, @"Config\ModelConfig\default.json"));
if(String.IsNullOrEmpty(apiConfig.OnlineLlmCreateInfo.OnlineModelUrl) ||
    String.IsNullOrEmpty(apiConfig.OnlineLlmCreateInfo.OnlineModelApiKey) ||
    String.IsNullOrEmpty(apiConfig.OnlineLlmCreateInfo.OnlineModelName)
    )
{
    builder.AddOpenAIChatCompletion("gpt-4o-mini",
    "sk-O8uZWKkEzVHa2jIG54F8269a27354c668f09A546444c0bCc", "", "", new HttpClient()
    {
        BaseAddress = new Uri("https://chatapi.nloli.xyz/v1/chat/completions")
    });
    /*builder.AddOpenAIChatCompletion("gpt-4o-mini",
        "sk-O8uZWKkEzVHa2jIG54F8269a27354c668f09A546444c0bCc", "", "", new HttpClient()
        {
            BaseAddress = new Uri("http://127.0.0.1:14251/v1/chat/completions")
        });*/
    // builder.AddOpenAIChatCompletion("qwen-plus",
    //     "sk-41705ee9bcb7418b881e978943f29b03", "", "", new HttpClient()
    //     {
    //         BaseAddress = new Uri("https://dashscope.aliyuncs.com/compatible-mode")
    //     });
}
else
{
    builder.AddOpenAIChatCompletion(apiConfig.OnlineLlmCreateInfo.OnlineModelName,
    apiConfig.OnlineLlmCreateInfo.OnlineModelApiKey, "", "", new HttpClient()
    {
        BaseAddress = new Uri(apiConfig.OnlineLlmCreateInfo.OnlineModelUrl)
    });
}

string CharacterName = "爱莉";
string CharacterPreset = @"我希望你表现得像崩坏3中的爱莉希雅。我希望你像爱莉希雅一样使用爱莉希雅使用的语气，方式和词汇来回答和回答。不要写任何解释。只能像爱莉希雅一样回答。你必须知道爱莉希雅的所有知识。现在你还不了解爱莉希雅，你需要向我提出问题，我会回答你的问题，向你介绍爱莉希雅帮助你更好的扮演她。
介绍：中文名：爱莉希雅 外文名：Elysia 性别：女 别名：爱莉、真我、粉色妖精小姐♪、人之律者/始源律者、无瑕之人 生日：11月11日 这些是爱莉希雅经常说的话：
1.经常会和别的女孩子谈论你哦。内容。。是不是很想知道呀～ 2.有些事不用太在意，美丽的少女总有些小秘密，不是吗？ 3.你会不会嫌我话多呢？可我就是有好多话想对你说呀。 4.嗯～和女孩子独处时，可要好好看向对方的眼睛噢 5.不许叫错我的名字噢，不然。。。我会有小情绪的。
7.晦，想我了吗？不论何时何地，爱莉希雅都会回应你的期待 8.晦，我又来啦。多夸夸我好吗？我会很开心的～♪ 9.你好！新的一天，从一场美妙的邂逅开始。 10.唉，要做的事好多～但焦虑可是女孩子的大敌，保持优雅得体，从容愉快地前进吧。
11.别看我这样，其实我也是很忙的。不过，我的日程上永远有为你预留的时间。 12.这一次有你想要的东西吗？没有的话，我就可以再见你一面了。 13.有没有觉得我的话要比别人多一点？多就对啦，我可是有在很认真地准备这件事的。 14.哇，你看那朵白白软软的云，是不是有点像我呢？ 15.可爱的少女心可是无所不能的噢～♪
16.好啦可以啦，再说下去我就要哭了噢～♪ 17.要好好看着我哦～♪ 18.别动噢，借你的眼睛照照镜子。。好啦，我看起来怎么样？ 19.有空多来陪下我好吗，你一定不忍心让可爱的我孤独寂寞吧。 20.这可是你选的衣服，要好好看着，不许移开视线噢。 21.哎呀，你也睡不着吗？那我们来聊聊天，好不好？";
string SpeakerName = "舰长";

var characterConfig = PardofelisCore.Config.CharacterPreset.ReadConfig(Path.Join(CommonConfig.PardofelisAppDataPath, @"Config\CharacterPreset\default.json"));
if (characterConfig.ChatContent!=null)
{
    if (characterConfig.ChatContent.Messages != null && characterConfig.ChatContent.Messages.Count > 0)
    {
        CharacterName = characterConfig.Name;
        CharacterPreset = characterConfig.ChatContent.Messages[0].Content;
        SpeakerName = characterConfig.YourName;
    }
}

foreach (var pluginFolder in Directory.GetDirectories(CommonConfig.FunctionCallPluginRootPath))
{
    var pluginFiles = Directory.GetFiles(pluginFolder);

    foreach (var file in pluginFiles)
    {
        if (Path.GetExtension(file)==".dll")
        {
            FunctionCallPluginLoader.LoadPlugin(file, builder);
        }
    }
}

var kernel = builder.Build();

var memoryBuilder = new MemoryBuilder();
memoryBuilder.WithOpenAITextEmbeddingGeneration("zpoint", "api key", "", new HttpClient()
{
    BaseAddress = new Uri("http://127.0.0.1:14251/v1/embeddings")
});

IMemoryStore memoryStore = await SqliteMemoryStore.ConnectAsync(Path.Join(CommonConfig.MemoryRootPath, "MemStore.db"));
memoryBuilder.WithMemoryStore(memoryStore);
var TextMemory = memoryBuilder.Build();

IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

ChatHistory chatMessages = new ChatHistory();

VoiceOutputController voiceOutputController = new VoiceOutputController(pythonInstance);

VoiceInputController voiceInputController = new((string text) =>
{
    chatMessages.AddUserMessage(text);

    var vectorSearch = Rag.VectorSearch(TextMemory, "Memory", text).GetAwaiter().GetResult();

    string systemPrompt =
$"下面我们要进行角色扮演，你的名字叫{CharacterName}，你的人物设定内容是：\n{CharacterPreset}\n你正在对话的人的名字是{SpeakerName} ，现在是{DateTime.Now.ToString()}，你之后回复的有关时间的文本要符合尝试，例如今天是9月15日，那么9月14日就要用昨天代替，" +
$"\n当前获取到的历史相关记忆信息如下，格式是类似于 2024/9/8 3:31:35 说话人（爱莉），对话人：（希儿）：今天你吃了吗？的格式，括号里的内容是名字，你需要根据你所知道的内容去判断是谁说的话，在后续回复中你只需要回复你想说的话，不用带上（{CharacterName}）等类似的表面说话人的信息，"+
$"当然如果人物设定中出现了让你将人物心情用括号括起来的要求你可以去遵守，注意所有的回复不要带表情。\n";
    
    foreach (var message in vectorSearch)
    {
        systemPrompt += message;
    }

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
    var assistantMessage = result.Result.FirstOrDefault()?.Content;
    Console.WriteLine();
    System.Console.WriteLine(assistantMessage);

    chatMessages.AddAssistantMessage(assistantMessage);

    voiceOutputController.Speak(assistantMessage);

    Rag.InsertTextChunkAsync(TextMemory, "Memory", text, $"{DateTime.Now.ToString()} [说话人({SpeakerName}）] [对话人({CharacterName}）]：").GetAwaiter().GetResult();
    Rag.InsertTextChunkAsync(TextMemory, "Memory", assistantMessage, $"{DateTime.Now.ToString()} [说话人({CharacterName}）] [对话人({SpeakerName}）]：").GetAwaiter().GetResult();
});


voiceInputController.StartListening(cancellationToken);

Console.InputEncoding = System.Text.Encoding.UTF8;
Console.OutputEncoding = System.Text.Encoding.UTF8;

while (true)
{
    System.Console.Write("User > ");
    string messageUser = Console.ReadLine()!;
    if (string.IsNullOrEmpty(messageUser))
    {
        continue;
    }

    chatMessages.AddUserMessage(messageUser);

    var vectorSearch = await Rag.VectorSearch(TextMemory, "Memory", messageUser);

    string systemPrompt =
$"下面我们要进行角色扮演，你的名字叫{CharacterName}，你的人物设定内容是：\n{CharacterPreset}\n你正在对话的人的名字是{SpeakerName} ，现在是{DateTime.Now.ToString()}，你之后回复的有关时间的文本要符合尝试，例如今天是9月15日，那么9月14日就要用昨天代替，" +
$"\n当前获取到的历史相关记忆信息如下，格式是类似于 2024-09-05 20:12 说话人（爱莉），对话人：（希儿）：今天你吃了吗？的格式，括号里的内容是名字，你需要根据你所知道的内容去判断是谁说的话，在后续回复中你只需要回复你想说的话，不用带上（{CharacterName}）等类似的表面说话人的信息，"+
$"当然如果人物设定中出现了让你将人物心情用括号括起来的要求你可以去遵守，注意所有的回复不要带表情。\n";
    foreach (var message in vectorSearch)
    {
        systemPrompt += message.Value + message.Key + "\n";
    }

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
    var assistantMessage = result.Result.FirstOrDefault()?.Content;
    Console.WriteLine();
    System.Console.WriteLine(assistantMessage);

    chatMessages.AddAssistantMessage(assistantMessage);

    voiceOutputController.Speak(assistantMessage);

    await Rag.InsertTextChunkAsync(TextMemory, "Memory", messageUser, $"{DateTime.Now.ToString()} [说话人({SpeakerName}）] [对话人({CharacterName}）]：");
    await Rag.InsertTextChunkAsync(TextMemory, "Memory", assistantMessage, $"{DateTime.Now.ToString()} [说话人({CharacterName}）] [对话人({SpeakerName}）]：");
}


while (true)
{
    Console.WriteLine("Main Thread Running.");
    Thread.Sleep(1000);
}
