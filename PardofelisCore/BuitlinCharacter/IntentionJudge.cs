using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PardofelisCore.Config;
using PardofelisCore.Util;
using Serilog;

namespace PardofelisCore.BuitlinCharacter;

public class IntentionJudge
{
    private static List<ChatMessage> _messages = new List<ChatMessage>()
    {
        new ChatMessage(
            Role.System,
            "请你判断对话的意图，然后将其尽可能以简洁干练的语言进行总结。\n例如以下对话：\nA:你好\nB:你好呀~ 最近有什么新鲜事可以和我分享一下吗？\nA:最近看了场电影\nB:什么电影啊？好看么？\n\n你应该将其进行如下总结：\nA正在分享最近的生活经历，想要与B讨论他最近看的电影。\nA似乎乐于分享最近的经历，而B则表现出对用户生活的兴趣，并对他们所提及的电影感兴趣。整体氛围是轻松和愉快的。"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的意图，然后将其尽可能以简洁干练的语言进行总结。这里是你需要判断意图的文本：\nA:你好\nB:你好呀~ 最近有什么新鲜事可以和我分享一下吗？\nA:最近看了场电影\nB:什么电影啊？好看么？"
        ),
        new ChatMessage(
            Role.Assistant,
            "A正在分享最近的生活经历，想要与B讨论他最近看的电影。\nA似乎乐于分享最近的经历，而B则表现出对用户生活的兴趣，并对他们所提及的电影感兴趣。整体氛围是轻松和愉快的。"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的意图，然后将其尽可能以简洁干练的语言进行总结。这里是你需要判断意图的文本：\nA:下午好\nB:你好呀~\nA:你在做什么呢？"
        ),
        new ChatMessage(
            Role.Assistant,
            "A与B互相打了招呼\nA询问B正在做什么？整体氛围是轻松和愉快的。"
        )
    };
    
    public static ResultWrap<string> ExecIntentionJudge(Kernel semanticKernel, string userMessage,
        ChatHistory chatHistory, string nameA, string nameB)
    {
        var character = MessageConvert.ChatMessageToChatMessages(new ChatContent()
        {
            YourName = nameA,
            CharacterName = nameB,
            Messages = _messages
        });

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

        var dialogue = MessageConvert.FormatDialogue(labHistory, nameA, nameB);

        character.AddUserMessage("请你判断对话的意图，然后将其尽可能以简洁干练的语言进行总结。这里是你需要判断意图的文本：\n" + dialogue);

        IChatCompletionService chatCompletionService = semanticKernel.GetRequiredService<IChatCompletionService>();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            Temperature = 0.7f,
        };

        Task<IReadOnlyList<ChatMessageContent>>? result = null;
        string assistantMessage = "";
        string errorMessage = "";
        try
        {
            result = chatCompletionService.GetChatMessageContentsAsync(
                character,
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
                    return new ResultWrap<string>(true, assistantMessage);
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
}