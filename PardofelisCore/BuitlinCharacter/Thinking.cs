using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PardofelisCore.Config;
using PardofelisCore.Util;
using Serilog;

namespace PardofelisCore.BuitlinCharacter;

/*"clothes": "姬松学园校服",
"location": "在自己的房间",
"emotion": "平静",
"favorability": "5/100",
"relationship": "陌生人",
"summarize": "我是绫地宁宁，是一个高中二年级女生。"*/
public class CharacterState
{
    [JsonProperty("穿着")]
    public string clothes;
    
    [JsonProperty("位置")]
    public string location;
    
    [JsonProperty("心情")]
    public string emotion;
    
    [JsonProperty("好感度")]
    public string favorability;
    
    [JsonProperty("与对话者的关系")]
    public string relationship;
    
    [JsonProperty("summarize")]
    public string summarize;

    public CharacterState(string clothes, string location, string emotion, string favorability, string relationship,
        string summarize)
    {
        this.clothes = clothes;
        this.location = location;
        this.emotion = emotion;
        this.favorability = favorability;
        this.relationship = relationship;
        this.summarize = summarize;
    }
}

public class Thinking
{
    public static ResultWrap<string> ExecInternalThinking(Kernel semanticKernel, string systemPrompt,
        string summarize,
        CharacterState oldState,
        string intentionJudge,
        string userMessage,
        ChatHistory chatHistory,
        string nameA, string nameB)
    {
        ChatHistory labHistory = new();
        foreach (var message in chatHistory)
        {
            switch (message.Role.Label)
            {
                case "user":
                    labHistory.AddUserMessage(message.Content);
                    break;
                case "assistant":
                    labHistory.AddAssistantMessage(message.Content);
                    break;
                case "system":
                    labHistory.AddSystemMessage(message.Content);
                    break;
            }
        }

        labHistory.AddUserMessage(userMessage);


        string think = $"""
                        {summarize}
                        当前时间：{DateTime.Now}
                        当前穿着：{oldState.clothes}
                        当前位置：{oldState.location}
                        当前心情：{oldState.emotion}
                        当前好感度：{oldState.favorability}
                        与对话者的关系：{oldState.relationship}
                        对历史记录的分析：
                        {intentionJudge}
                        以下是我看到消息后的内心思考(内心思考为一整个段落没有换行)：
                        我思考了一下，
                        """;
        labHistory.AddAssistantMessage(think);
        labHistory.AddUserMessage("（继续将你的思考内容补充完毕，只需要思考的内容）");
        
        IChatCompletionService chatCompletionService = semanticKernel.GetRequiredService<IChatCompletionService>();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ChatSystemPrompt = systemPrompt,
            Temperature = 0.7f,
            StopSequences = new List<string>
            {
                "以下是", "：", ":", "\n"
            }
        };

        Task<IReadOnlyList<ChatMessageContent>>? result = null;
        string assistantMessage = "";
        string errorMessage = "";
        try
        {
            result = chatCompletionService.GetChatMessageContentsAsync(
                labHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: semanticKernel);
            if (result != null)
            {
                assistantMessage = result.Result.FirstOrDefault()?.Content;
                if (string.IsNullOrEmpty(assistantMessage))
                {
                    return new ResultWrap<string>(false, "请求大模型数据失败!");
                }
                else
                {
                    string output = Regex.Replace(assistantMessage, @"\n\s*\n", "\n");

                    string[] splitOutput = output.Split(new string[] { "\n" },
                        StringSplitOptions.None);
                    string sendOutput = splitOutput.Length > 0 ? splitOutput[0] : string.Empty;
                    sendOutput = sendOutput.Replace("：", "。");
                    sendOutput = sendOutput.Replace(":", "。");
                    
                    Console.WriteLine("内心思想：" + sendOutput);

                    return new ResultWrap<string>(true, sendOutput);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            errorMessage = e.Message;
            return new ResultWrap<string>(false, errorMessage);
            //MessageBoxUtil.ShowMessageBox("请求大模型数据失败! 错误信息：" + e.Message, "确定")
            //    .GetAwaiter().GetResult();
        }

        return new ResultWrap<string>(false, errorMessage);
    }

    public static ResultWrap<string> ExecAction(Kernel semanticKernel, string systemPrompt,
        string summarize,
        CharacterState oldState,
        string intentionJudge,
        string intentionThink,
        string userMessage,
        ChatHistory chatHistory,
        string nameA, string nameB)
    {
        ChatHistory labHistory = new();
        foreach (var message in chatHistory)
        {
            switch (message.Role.Label)
            {
                case "user":
                    labHistory.AddUserMessage(message.Content);
                    break;
                case "assistant":
                    labHistory.AddAssistantMessage(message.Content);
                    break;
                case "system":
                    labHistory.AddSystemMessage(message.Content);
                    break;
            }
        }

        labHistory.AddUserMessage(userMessage);


        string think = $"""
                        {summarize}
                        当前时间：{DateTime.Now}
                        当前穿着：{oldState.clothes}
                        当前位置：{oldState.location}
                        当前心情：{oldState.emotion}
                        当前好感度：{oldState.favorability}
                        与对话者的关系：{oldState.relationship}
                        对历史记录的分析：
                        {intentionJudge}
                        以下是我看到消息后的内心思考：
                        {intentionThink}
                        于是我看到消息后做了如下动作(只有我自己的身体动作情况，且动作描写为一整个段落没有换行，例如：{nameB}调整了一下坐姿，拿起手机，发送了一条消息。)：
                        {nameB}
                        """;
        labHistory.AddAssistantMessage(think);
        labHistory.AddUserMessage("（继续将你的要做的动作内容补充完毕，只需要动作内容）");


        IChatCompletionService chatCompletionService = semanticKernel.GetRequiredService<IChatCompletionService>();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ChatSystemPrompt = systemPrompt,
            Temperature = 0.7f,
            StopSequences = new List<string>
            {
                "以下是", "：", ":", "\n"
            }
        };

        Task<IReadOnlyList<ChatMessageContent>>? result = null;
        string assistantMessage = "";
        string errorMessage = "";
        try
        {
            result = chatCompletionService.GetChatMessageContentsAsync(
                labHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: semanticKernel);
            if (result != null)
            {
                assistantMessage = result.Result.FirstOrDefault()?.Content;
                if (string.IsNullOrEmpty(assistantMessage))
                {
                    return new ResultWrap<string>(false, "请求大模型数据失败!");
                }
                else
                {
                    string output = Regex.Replace(assistantMessage, @"\n\s*\n", "\n");

                    string[] splitOutput =
                        output.Split(
                            new string[]
                            {
                                "\n"
                            }, StringSplitOptions.None);
                    string sendOutput = splitOutput.Length > 0 ? splitOutput[0] : string.Empty;
                    sendOutput = sendOutput.Replace("：", "。");
                    sendOutput = sendOutput.Replace(":", "。");

                    Console.WriteLine("动作：" + sendOutput);

                    return new ResultWrap<string>(true, sendOutput);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            errorMessage = e.Message;
            return new ResultWrap<string>(false, errorMessage);
            //MessageBoxUtil.ShowMessageBox("请求大模型数据失败! 错误信息：" + e.Message, "确定")
            //    .GetAwaiter().GetResult();
        }

        return new ResultWrap<string>(false, errorMessage);
    }

    public static ResultWrap<CharacterState> ExecStatusBar(Kernel semanticKernel, string systemPrompt,
        string summarize,
        CharacterState oldState,
        string intentionJudge,
        string intentionThink,
        string action,
        string userMessage,
        ChatHistory chatHistory,
        string nameA, string nameB)
    {
        ChatHistory labHistory = new();
        foreach (var message in chatHistory)
        {
            switch (message.Role.Label)
            {
                case "user":
                    labHistory.AddUserMessage(message.Content);
                    break;
                case "assistant":
                    labHistory.AddAssistantMessage(message.Content);
                    break;
                case "system":
                    labHistory.AddSystemMessage(message.Content);
                    break;
            }
        }

        labHistory.AddUserMessage(userMessage);


        string think = $$"""
                        {{summarize}}
                        当前时间：{{DateTime.Now}}
                        当前穿着：{{oldState.clothes}}
                        当前位置：{{oldState.location}}
                        当前心情：{{oldState.emotion}}
                        当前好感度：{{oldState.favorability}}
                        与对话者的关系：{{oldState.relationship}}
                        对历史记录的分析：
                        {{intentionJudge}}
                        以下是我看到消息后的内心思考：
                        {{intentionThink}}
                        以下是我看到消息后做出的动作：
                        {{action}}
                        以下是我在看到消息前，输出的标准json格式数值(好感度最高每次增减5)：
                        {
                            "穿着": "{{oldState.clothes}}",
                            "位置": "{{oldState.location}}",
                            "心情": "{{oldState.emotion}}",
                            "好感度": "{{oldState.favorability}}",
                            "与对话者的关系": "{{oldState.relationship}}"
                        }
                        以下是我在看到消息后，输出的标准json格式数值(好感度最高每次增减5)：
                        """;
        labHistory.AddAssistantMessage(think);
        labHistory.AddUserMessage("（继续将输出的标准json补充完毕，只需要json的内容）");

        IChatCompletionService chatCompletionService = semanticKernel.GetRequiredService<IChatCompletionService>();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ChatSystemPrompt = systemPrompt,
            Temperature = 0.7f,
            StopSequences = new List<string>
            {
                "}"
            },
        };

        Task<IReadOnlyList<ChatMessageContent>>? result = null;
        string assistantMessage = "";
        string errorMessage = "";
        try
        {
            result = chatCompletionService.GetChatMessageContentsAsync(
                labHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: semanticKernel);
            if (result != null)
            {
                assistantMessage = result.Result.FirstOrDefault()?.Content;
                
                int startIndex = assistantMessage.IndexOf('{');
                if (startIndex != -1)
                {
                    assistantMessage = assistantMessage.Substring(startIndex);
                }
                
                if (!assistantMessage.Contains("}"))
                {
                    assistantMessage += "}";
                }
                
                if (string.IsNullOrEmpty(assistantMessage))
                {
                    return new ResultWrap<CharacterState>(false, oldState);
                }
                else
                {
                    string output = Regex.Replace(assistantMessage, @"\n\s*\n", "\n");

                    string sendOutput = output;
                    
                    var results = JsonConvert.DeserializeObject<CharacterState>(sendOutput);

                    var formattedResults = $"""
                                            状态栏:
                                            | 穿着: {results.clothes} |
                                            | 位置: {results.location} |
                                            | 心情: {results.emotion} |
                                            | 好感度: {results.favorability} |
                                            | 与对话者的关系: {results.relationship} |
                                            """;
                    
                    Console.WriteLine("状态栏：" + formattedResults);

                    return new ResultWrap<CharacterState>(true, results);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            errorMessage = e.Message;
            return new ResultWrap<CharacterState>(false, oldState);
            //MessageBoxUtil.ShowMessageBox("请求大模型数据失败! 错误信息：" + e.Message, "确定")
            //    .GetAwaiter().GetResult();
        }

        return new ResultWrap<CharacterState>(false, oldState);
    }
}