namespace PardofelisCore.LlmController.OpenAiModel;

/// 嵌入请求
/// https://platform.openai.com/docs/api-reference/embeddings/create
public class EmbeddingRequest
{
    /// 输入文本进行嵌入，编码为字符串或令牌数组。要在单个请求中嵌入多个输入，传递字符串数组或令牌数组数组。
    /// 输入不能超过模型的最大输入令牌（对于8192个令牌），不能是空字符串，任何数组必须是2048维或更少。
    public string[] input { get; set; } = Array.Empty<string>();
    
    /// 要使用的模型的ID。您可以使用列表模型API查看所有可用的模型，或查看我们的模型概述以获取它们的描述。
    public string model { get; set; } = string.Empty;
    
    /// 返回嵌入的格式。可以是float或base64
    public string encoding_format { get; set; } = "float";
    
    /// 结果输出嵌入应具有的维数。仅在text-embedding-3及更高版本的模型中受支持。
    public int? dimensions { get; set; }
    
    /// 表示您的最终用户的唯一标识符，可以帮助OpenAI监控和检测滥用
    public string? user { get; set; }
}

public class EmbeddingObject
{
    /// 嵌入在嵌入列表中的索引
    public int index { get; set; } = 0;
    
    /// 嵌入向量，是浮点数列表。向量的长度取决于模型，如嵌入指南中所列
    public float[] embedding { get; set; }
    
    /// 对象类型，始终为"embedding"
    public string @object { get; set; } = "embedding";
}

/// 返回嵌入的响应
public class EmbeddingResponse
{
    /// 对象类型，始终为list
    public string @object { get; set; } = "list";
    
    /// 嵌入对象列表
    public EmbeddingObject[] data { get; set; }
    
    /// 用于嵌入的模型
    public string model { get; set; } = string.Empty;
    
    /// 使用统计信息
    public EmbeddingTokenUsageInfo usage { get; set; } = new();
}
