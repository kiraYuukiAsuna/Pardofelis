namespace PardofelisCore.LlmController.OpenAiModel;

/// 推理完成令牌信息
public class TokenUsageInfo
{
    /// 提示令牌数
    public int prompt_tokens { get; set; } = 0;
    
    /// 完成令牌数
    public int completion_tokens { get; set; } = 0;
    
    /// 总令牌数
    public int total_tokens { get; set; } = 0;
}

/// Embedding 完成令牌信息
public class EmbeddingTokenUsageInfo
{
    /// 提示令牌数
    public int prompt_tokens { get; set; } = 0;
    
    /// 总令牌数
    public int total_tokens { get; set; } = 0;
}
