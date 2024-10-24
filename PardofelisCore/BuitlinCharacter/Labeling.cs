﻿using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using PardofelisCore.Config;
using PardofelisCore.Util;
using Serilog;
using ChatMessage = PardofelisCore.Config.ChatMessage;

namespace PardofelisCore.BuitlinCharacter;

public class Labeling
{
    private static List<ChatMessage> _messages = new List<ChatMessage>()
    {
        new ChatMessage(
            Role.System,
            "关注对话中的关键信息和主要讨论主题。提供至少5个标签。不要包含任何附加信息、评论或解释。标签应格式为单词或简短短语，每个标签不超过10个字。只需要返回标签就行。\n例如以下对话：\nA:你好\nB:你好呀~ 最近有什么新鲜事可以和我分享一下吗？\nA:最近看了场电影\nB:什么电影啊？好看么？\nA:看了《奥本海默》\nB:是灾难片么？没听说过。\nA:不，是类似纪录片的形式。\nB:好看么？\nA:还挺好看的\nB:哦，那我有空了也想看看，你想和我再看一遍么?\n\n你应该提供如下：\n标签：打招呼，新鲜事，看电影，奥本海默，一起看"
        )
    };

    public static ResultWrap<string> ExecLabeling(Kernel semanticKernel, string userMessage,
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

        character.AddUserMessage(dialogue);

        IChatCompletionService chatCompletionService = semanticKernel.GetRequiredService<IChatCompletionService>();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            Temperature = 0.7f,
        };

        Task<IReadOnlyList<Microsoft.SemanticKernel.ChatMessageContent>>? result = null;
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