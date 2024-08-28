namespace PardofelisCore.LlmController.LlamaSharpWrapper.Service;

/// 消息历史记录结果
public class ChatHistoryResult
{
    /// 历史记录
    public string ChatHistory { get; set; }

    /// 是否启用了工具提示
    public bool IsToolPromptEnabled { get; set; }

    /// 工具结束标记
    public string[]? ToolStopWords { get; set; }

    public ChatHistoryResult(string chatHistory, bool isToolPromptEnabled, string[]? toolStopWords)
    {
        ChatHistory = chatHistory;
        IsToolPromptEnabled = isToolPromptEnabled;
        ToolStopWords = toolStopWords;
    }
}