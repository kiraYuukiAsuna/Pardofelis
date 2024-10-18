using Microsoft.SemanticKernel.ChatCompletion;
using PardofelisCore.Config;

namespace PardofelisCore.Util;

public class MessageConvert
{
    public static string FormatDialogue(ChatHistory chatHistory, string nameA, string nameB)
    {
        string result = "";

        foreach (var message in chatHistory)
        {
            switch (message.Role.Label)
            {
                case "user":
                    result += nameA + ": " + message.Content + "\n";
                    break;
                case "assistant":
                    result += nameB + ": " + message.Content + "\n";
                    break;
            }
        }

        return result;
    }
    
    public static ChatContent ChatMessagesToChatMessage(ChatHistory chatMessages)
    {
        ChatContent chatContent = new();
        foreach (var message in chatMessages)
        {
            switch (message.Role.Label)
            {
                case "system":
                {
                    if (string.IsNullOrEmpty(message.Content))
                    {
                        break;
                    }

                    chatContent.Messages.Add(new PardofelisCore.Config.ChatMessage
                    (
                        Role.System,
                        message.Content
                    ));
                    break;
                }

                case "assistant":
                {
                    if (string.IsNullOrEmpty(message.Content))
                    {
                        break;
                    }

                    chatContent.Messages.Add(new PardofelisCore.Config.ChatMessage
                    (
                        Role.Assistant,
                        message.Content
                    ));
                    break;
                }
                case "user":
                {
                    if (string.IsNullOrEmpty(message.Content))
                    {
                        break;
                    }

                    chatContent.Messages.Add(new PardofelisCore.Config.ChatMessage
                    (
                        Role.User,
                        message.Content
                    ));
                    break;
                }
            }
        }

        return chatContent;
    }
    
    public static ChatHistory ChatMessageToChatMessages(ChatContent chatContent)
    {
        ChatHistory chatMessages = new();
        foreach (var message in chatContent.Messages)
        {
            switch (message.Role)
            {
                case Role.System:
                {
                    if (string.IsNullOrEmpty(message.Content))
                    {
                        break;
                    }

                    chatMessages.AddSystemMessage(
                        message.Content);
                    break;
                }

                case Role.Assistant:
                {
                    if (string.IsNullOrEmpty(message.Content))
                    {
                        break;
                    }

                    chatMessages.AddAssistantMessage(
                        message.Content);
                    break;
                }
                case Role.User:
                {
                    if (string.IsNullOrEmpty(message.Content))
                    {
                        break;
                    }

                    chatMessages.AddUserMessage(
                        message.Content);
                    break;
                }
            }
        }

        return chatMessages;
    }
}