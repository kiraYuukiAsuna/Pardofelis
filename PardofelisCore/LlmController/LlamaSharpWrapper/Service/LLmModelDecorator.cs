using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PardofelisCore.Config;
using PardofelisCore.LlmController.LlamaSharpWrapper.FunctionCall;
using PardofelisCore.LlmController.OpenAiModel;
using Serilog;
using Timer = System.Timers.Timer;

namespace PardofelisCore.LlmController.LlamaSharpWrapper.Service;

/// LLM 模型服务 装饰器类
public class LLmModelDecorator : ILlmModelService
{
    private readonly ILlmModelService _llmService;
    private readonly Serilog.ILogger _logger;

    private readonly ToolPromptGenerator _toolPromptGenerator;

    public bool IsSupportEmbedding => _llmService.IsSupportEmbedding;

    public LLmModelDecorator()
    {
        _logger = new LoggerConfiguration().WriteTo.Console()
            .WriteTo.File(Path.Join(CommonConfig.LogRootPath, "LogLLmModelDecorator.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        _toolPromptGenerator = new ToolPromptGenerator();
        _llmService = new LlmModelService();

        // 定时器
        if (GlobalConfig.Instance.AutoReleaseTime > 0)
        {
            _logger.Information("Auto release time: {time} min.", GlobalConfig.Instance.AutoReleaseTime);
            _idleThreshold = TimeSpan.FromMinutes(GlobalConfig.Instance.AutoReleaseTime);
            _lastUsedTime = DateTime.Now;
            _idleTimer = new Timer(60000); // 每分钟检查一次
            _idleTimer.Elapsed += CheckIdle;
            _idleTimer.Start();
        }
    }

    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            BeginUseModel();
            return await _llmService.CreateChatCompletionAsync(request, cancellationToken);
        }
        finally
        {
            EndUseModel();
            _lastUsedTime = DateTime.Now;
        }
    }

    public async IAsyncEnumerable<string> CreateChatCompletionStreamAsync(ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            BeginUseModel();
            await foreach (var item in _llmService.CreateChatCompletionStreamAsync(request, cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            EndUseModel();
            _lastUsedTime = DateTime.Now;
        }
    }

    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            BeginUseModel();
            return await _llmService.CreateCompletionAsync(request, cancellationToken);
        }
        finally
        {
            EndUseModel();
            _lastUsedTime = DateTime.Now;
        }
    }

    public async IAsyncEnumerable<string> CreateCompletionStreamAsync(CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            BeginUseModel();
            await foreach (var item in _llmService.CreateCompletionStreamAsync(request, cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            EndUseModel();
            _lastUsedTime = DateTime.Now;
        }
    }

    public async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            BeginUseModel();
            return await _llmService.CreateEmbeddingAsync(request, cancellationToken);
        }
        finally
        {
            EndUseModel();
            _lastUsedTime = DateTime.Now;
        }
    }

    public IReadOnlyDictionary<string, string> GetModelInfo()
    {
        try
        {
            BeginUseModel();
            return _llmService.GetModelInfo();
        }
        finally
        {
            EndUseModel();
            _lastUsedTime = DateTime.Now;
        }
    }

    public void InitModelIndex()
    {
        if (GlobalConfig.Instance.IsModelLoaded && _modelUsageCount != 0)
        {
            _logger.Warning("Model is in use.");
            throw new InvalidOperationException("Model is in use.");
        }

        _llmService.InitModelIndex();
    }

    public void Dispose()
    {
        _llmService.Dispose();
    }

    public void DisposeModel()
    {
        _llmService.DisposeModel();
    }

    // 资源释放计时器
    private Timer? _idleTimer;
    private DateTime _lastUsedTime;
    private readonly TimeSpan _idleThreshold;

    // 模型使用计数
    // 暂未使用
    private int _modelUsageCount = 0;

    /// 模型使用计数 - 开始
    public void BeginUseModel()
    {
        // 模型未加载时，初始化模型
        if (!GlobalConfig.Instance.IsModelLoaded)
        {
            _llmService.InitModelIndex();
        }

        Interlocked.Increment(ref _modelUsageCount);
    }

    /// 模型使用计数 - 结束
    public void EndUseModel()
    {
        Interlocked.Decrement(ref _modelUsageCount);
    }

    /// 模型自动释放检查
    private void CheckIdle(object? sender, object e)
    {
        if (DateTime.Now - _lastUsedTime > _idleThreshold && GlobalConfig.Instance.IsModelLoaded && _modelUsageCount == 0)
        {
            _logger.Information("Auto release model.");
            DisposeModel();
        }
    }
}