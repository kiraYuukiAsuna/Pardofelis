using System.Text.Json.Serialization;

namespace PardofelisCore.LlmController.OpenAiModel;

/// 对话消息列表
public class ChatCompletionMessage
{
    /// 角色
    /// system, user, assistant, tool
    public string? role { get; set; } = string.Empty;
   
    /// 对话内容
    public string? content { get; set; }

    /// 工具调用信息
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolMessage[]? tool_calls { get; set; }
    
    /// 调用工具的 ID
    /// role 为 tool 时必填
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? tool_call_id { get; set; }
    
}

/// 对话完成请求
/// https://platform.openai.com/docs/api-reference/chat/create
public class ChatCompletionRequest : BaseCompletionRequest
{
    /// 对话历史
    public ChatCompletionMessage[] messages { get; set; } = Array.Empty<ChatCompletionMessage>();
    
    /// 控制模型是否调用某个工具，以及如何调用。
    /// 可以是字符串（"none", "auto", "required", "parallel"）或一个指定工具的对象。
    /// {"type": "function", "function": {"name": "my_function"}}
    /// 默认为"none"，表示模型不会调用任何工具，而是生成一条消息。
    /// 如果存在工具，则默认为"auto"。
    /// required，表示必须调用一个或多个工具
    /// parallel，表示并行调用多个工具
    public object? tool_choice { get; set; }
    
    /// 模型可能调用的工具列表。目前，仅支持函数作为工具。
    /// 使用它可以提供模型可以为其生成JSON输入的函数列表。
    /// 最多支持128个功能。
    public ToolInfo[]? tools { get; set; }
}

/// 完成的一种选择
public class ChatCompletionResponseChoice : BaseCompletionResponseChoice
{
    /// 模型生成的聊天完成消息
    public ChatCompletionMessage message { get; set; } = new();
}

/// 聊天完成响应
public class ChatCompletionResponse : BaseCompletionResponse
{
    /// 对象类型，始终为chat.completion
    public string @object = "chat.completion";
    
    /// 聊天完成选择的列表。如果n大于1，则可以有多个
    public ChatCompletionResponseChoice[] choices { get; set; } = Array.Empty<ChatCompletionResponseChoice>(); 
}

/// 流式响应完成的详情
public class ChatCompletionChunkResponseChoice : BaseCompletionResponseChoice
{
    /// 由流式模型响应生成的聊天完成增量。
    public ChatCompletionMessage? delta { get; set; } = new();
}

/// 流式响应的聊天完成响应
/// https://platform.openai.com/docs/api-reference/chat/streaming
public class ChatCompletionChunkResponse : BaseCompletionResponse
{
    /// 对象类型，始终为chat.completion.chunk
    public string @object = "chat.completion.chunk";
    
    /// 聊天完成选择的列表。如果n大于1，则可以有多个
    public ChatCompletionChunkResponseChoice[] choices { get; set; } = Array.Empty<ChatCompletionChunkResponseChoice>();

}
