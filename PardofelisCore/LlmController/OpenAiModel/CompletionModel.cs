namespace PardofelisCore.LlmController.OpenAiModel;

/// 提示完成请求
/// https://platform.openai.com/docs/api-reference/completions
public class CompletionRequest : BaseCompletionRequest
{
    /// 提示词
    /// 要为字符串、字符串数组、令牌数组或令牌数组数组生成补全的提示。
    public string prompt { get; set; } = string.Empty;
}

/// 完成的一种选择
public class CompletionResponseChoice : BaseCompletionResponseChoice
{
    /// 模型生成的完成消息
    public string? text { get; set; } = string.Empty;
}

/// 聊天完成响应
public class CompletionResponse : BaseCompletionResponse
{
    /// 对象类型，始终为text_completion
    public string @object = "text_completion";

    /// 聊天完成选择的列表。如果n大于1，则可以有多个
    public CompletionResponseChoice[] choices { get; set; } = Array.Empty<CompletionResponseChoice>();
}