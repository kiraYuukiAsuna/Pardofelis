using PardofelisCore.LlmController.OpenAiModel;

namespace PardofelisCore.LlmController.LlamaSharpWrapper.Service;

/// LLM 模型服务 接口
public interface ILlmModelService : IDisposable
{
    /// 是否支持嵌入
    bool IsSupportEmbedding { get; }

    /// 初始化指定模型
    void InitModelIndex();

    /// 主动释放模型资源
    void DisposeModel();

    /// 获取模型信息
    IReadOnlyDictionary<string, string> GetModelInfo();

    Task<ChatCompletionResponse> CreateChatCompletionAsync(ChatCompletionRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<string> CreateChatCompletionStreamAsync(ChatCompletionRequest request,
        CancellationToken cancellationToken);

    Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken);

    Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request, CancellationToken cancellationToken);

    IAsyncEnumerable<string>
        CreateCompletionStreamAsync(CompletionRequest request, CancellationToken cancellationToken);
}