using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using PardofelisCore.Config;
using PardofelisCore.LlmController.LlamaSharpWrapper.FunctionCall;
using PardofelisCore.LlmController.LlamaSharpWrapper.Transform;
using PardofelisCore.LlmController.OpenAiModel;
using Serilog;

namespace PardofelisCore.LlmController.LlamaSharpWrapper.Service;

public class LlmModelService : ILlmModelService
{
    private readonly Serilog.ILogger _logger;
    private readonly LlmModelConfigList _settings;
    private readonly ToolPromptGenerator _toolPromptGenerator;
    private LlmModelConfig _usedset;
    private LLamaWeights _model;
    private LLamaWeights _embeddingModel;
    private LLamaEmbedder? _embedder;

    // 已加载模型ID，-1表示未加载
    private int _loadModelIndex = -1;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public LlmModelService()
    {
        _settings = LlmModelConfigList.ReadConfig();
        _logger = new LoggerConfiguration().WriteTo.Console()
            .WriteTo.File(Path.Join(CommonConfig.LogRootPath, "LogLlmModelService.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        _toolPromptGenerator = new ToolPromptGenerator();
        InitModelIndex();
        if (_usedset == null || _model == null)
        {
            throw new InvalidOperationException("Failed to initialize the model.");
        }
    }
    
    /// 初始化指定模型
    public void InitModelIndex()
    {
        if (_settings.Models.Count == 0)
        {
            _logger.Error("No model settings.");
            throw new ArgumentException("No model settings.");
        }

        // 从配置中获取模型索引
        int loadModelIndex = GlobalConfig.Instance.CurrentModelIndex;

        // 检查模型是否已加载，且索引相同
        if (GlobalConfig.Instance.IsModelLoaded && _loadModelIndex == loadModelIndex)
        {
            _logger.Information("Model has been loaded.");
            return;
        }

        if (loadModelIndex < 0 || loadModelIndex >= _settings.Models.Count)
        {
            _logger.Error("Invalid model index: {modelIndex}.", loadModelIndex);
            throw new ArgumentException("Invalid model index.");
        }

        var usedset = _settings.Models[loadModelIndex];

        if (string.IsNullOrWhiteSpace(LlmModelParams.ToModelParams(usedset.LlmModelParams).ModelPath) ||
            !File.Exists(LlmModelParams.ToModelParams(usedset.LlmModelParams).ModelPath))
        {
            _logger.Error("Model path is error: {path}.", LlmModelParams.ToModelParams(usedset.LlmModelParams).ModelPath);
            throw new ArgumentException("Model path is error.");
        }

        // 适用于模型切换，先释放模型资源
        DisposeModel();

        _model = LLamaWeights.LoadFromFile(LlmModelParams.ToModelParams(usedset.LlmModelParams));

        var embeddingParams = LlmModelParams.ToModelParams(usedset.LlmModelParams);
        embeddingParams.PoolingType = LLamaPoolingType.Mean;
        /*var embeddingParams = new ModelParams(Path.Join(CommonConfig.EmbeddingModelRootPath, _settings.EmbeddingModelConfig.LlmModelParams.ModelFileName))
        {
            PoolingType = LLamaPoolingType.Mean,
            
        };*/
        _embeddingModel = LLamaWeights.LoadFromFile(embeddingParams);
        _embedder = new LLamaEmbedder(_embeddingModel, embeddingParams);
        
        /*if (LlmModelParams.ToModelParams(usedset.LlmModelParams).Embeddings)
        {
            _embedder = new LLamaEmbedder(_model, LlmModelParams.ToModelParams(usedset.LlmModelParams));
        }*/

        _usedset = usedset;
        _loadModelIndex = loadModelIndex;
        GlobalConfig.Instance.IsModelLoaded = true;
    }

    /// 获取模型信息
    public IReadOnlyDictionary<string, string> GetModelInfo()
    {
        return _model.Metadata;
    }

    #region ChatCompletion

    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        // 没有消息
        if (request.messages is null || request.messages.Length == 0)
        {
            _logger.Warning("No message in chat history.");
            return new ChatCompletionResponse();
        }

        var chatHistory = GetChatHistory(request);
        var genParams = GetInferenceParams(request, chatHistory.ToolStopWords);
        var ex = new MyStatelessExecutor(_model, LlmModelParams.ToModelParams(_usedset.LlmModelParams));
        var result = new StringBuilder();

        var completion_tokens = 0;

        _logger.Debug("Prompt context: {prompt_context}", chatHistory.ChatHistory);

        await foreach (var output in ex.InferAsync(chatHistory.ChatHistory, genParams, cancellationToken))
        {
            _logger.Debug("Message: {output}", output);
            result.Append(output);
            completion_tokens++;
        }

        var prompt_tokens = ex.PromptTokens;

        _logger.Debug("Prompt tokens: {prompt_tokens}, Completion tokens: {completion_tokens}", prompt_tokens,
            completion_tokens);
        _logger.Debug("Completion result: {result}", result);

        // 工具返回检测
        if (chatHistory.IsToolPromptEnabled)
        {
            var tools = _toolPromptGenerator.GenerateToolCall(result.ToString(), _usedset.ToolPrompt.Index);
            if (tools.Count > 0)
            {
                _logger.Debug("Tool calls: {tools}", tools.Select(x => x.name));
                return new ChatCompletionResponse
                {
                    id = $"chatcmpl-{Guid.NewGuid():N}",
                    model = request.model,
                    created = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    choices =
                    [
                        new ChatCompletionResponseChoice
                        {
                            index = 0,
                            finish_reason = "tool_calls",
                            message = new ChatCompletionMessage
                            {
                                role = "assistant",
                                tool_calls = tools.Select(x => new ToolMessage
                                {
                                    id = $"call_{Guid.NewGuid():N}",
                                    function = new ToolMeaasgeFuntion
                                    {
                                        name = x.name,
                                        arguments = x.arguments
                                    }
                                }).ToArray()
                            }
                        }
                    ],
                    usage = new UsageInfo
                    {
                        prompt_tokens = prompt_tokens,
                        completion_tokens = completion_tokens,
                        total_tokens = prompt_tokens + completion_tokens
                    }
                };
            }
        }

        return new ChatCompletionResponse
        {
            id = $"chatcmpl-{Guid.NewGuid():N}",
            model = request.model,
            created = DateTimeOffset.Now.ToUnixTimeSeconds(),
            choices =
            [
                new ChatCompletionResponseChoice
                {
                    index = 0,
                    message = new ChatCompletionMessage
                    {
                        role = "assistant",
                        content = result.ToString()
                    }
                }
            ],
            usage = new UsageInfo
            {
                prompt_tokens = prompt_tokens,
                completion_tokens = completion_tokens,
                total_tokens = prompt_tokens + completion_tokens
            }
        };
    }

    public async IAsyncEnumerable<string> CreateChatCompletionStreamAsync(ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 没有消息
        if (request.messages is null || request.messages.Length == 0)
        {
            _logger.Warning("No message in chat history.");
            yield break;
        }

        var chatHistory = GetChatHistory(request);
        var genParams = GetInferenceParams(request, chatHistory.ToolStopWords);
        var ex = new MyStatelessExecutor(_model, LlmModelParams.ToModelParams(_usedset.LlmModelParams));

        var id = $"chatcmpl-{Guid.NewGuid():N}";
        var created = DateTimeOffset.Now.ToUnixTimeSeconds();

        int index = 0;

        _logger.Debug("Prompt context: {prompt_context}", chatHistory.ChatHistory);

        // 第一个消息，带着角色名称
        var chunk = JsonSerializer.Serialize(new ChatCompletionChunkResponse
        {
            id = id,
            created = created,
            model = request.model,
            choices =
            [
                new ChatCompletionChunkResponseChoice
                {
                    index = index,
                    delta = new ChatCompletionMessage
                    {
                        role = "assistant"
                    },
                    finish_reason = null
                }
            ]
        }, _jsonSerializerOptions);
        yield return $"data: {chunk}\n\n";

        // 拦截前三个 token 存入
        List<string> tokens = new();
        // 工具返回是否激活
        bool toolActive = false;

        // 处理模型输出
        await foreach (var output in ex.InferAsync(chatHistory.ChatHistory, genParams, cancellationToken))
        {
            _logger.Debug("Message: {output}", output);

            // 存在工具提示时
            if (chatHistory.IsToolPromptEnabled)
            {
                // 激活工具提示后保持拦截，未激活时拦截前三个token
                if (toolActive || tokens.Count < 3)
                {
                    tokens.Add(output);
                    continue;
                }

                // 检查是否有工具提示
                toolActive = _toolPromptGenerator.IsToolActive(tokens, _usedset.ToolPrompt.Index);
                if (toolActive)
                {
                    _logger.Debug("Tool is active.");
                    // 激活，继续拦截
                    tokens.Add(output);
                    continue;
                }
                else
                {
                    // 未激活，变更工具启用状态
                    chatHistory.IsToolPromptEnabled = false;
                    // 并快速发送，之前的消息
                    foreach (var token in tokens)
                    {
                        chunk = JsonSerializer.Serialize(new ChatCompletionChunkResponse
                        {
                            id = id,
                            created = created,
                            model = request.model,
                            choices =
                            [
                                new ChatCompletionChunkResponseChoice
                                {
                                    index = ++index,
                                    delta = new ChatCompletionMessage
                                    {
                                        role = null,
                                        content = token
                                    },
                                    finish_reason = null
                                }
                            ]
                        }, _jsonSerializerOptions);
                        yield return $"data: {chunk}\n\n";
                    }
                }
            }

            chunk = JsonSerializer.Serialize(new ChatCompletionChunkResponse
            {
                id = id,
                created = created,
                model = request.model,
                choices =
                [
                    new ChatCompletionChunkResponseChoice
                    {
                        index = ++index,
                        delta = new ChatCompletionMessage
                        {
                            role = null,
                            content = output
                        },
                        finish_reason = null
                    }
                ],
            }, _jsonSerializerOptions);
            yield return $"data: {chunk}\n\n";
        }

        // 是否激活了工具提示拦截
        if (toolActive)
        {
            var result = string.Join("", tokens).Trim();
            var tools = _toolPromptGenerator.GenerateToolCall(result, _usedset.ToolPrompt.Index);
            if (tools.Count > 0)
            {
                _logger.Debug("Tool calls: {tools}", tools.Select(x => x.name));
                chunk = JsonSerializer.Serialize(new ChatCompletionChunkResponse
                {
                    id = id,
                    created = created,
                    model = request.model,
                    choices =
                    [
                        new ChatCompletionChunkResponseChoice
                        {
                            index = ++index,
                            delta = new ChatCompletionMessage
                            {
                                role = null,
                                tool_calls = tools.Select(x => new ToolMessage
                                {
                                    id = $"call_{Guid.NewGuid():N}",
                                    function = new ToolMeaasgeFuntion
                                    {
                                        name = x.name,
                                        arguments = x.arguments
                                    }
                                }).ToArray()
                            },
                            finish_reason = "tool_calls"
                        }
                    ]
                }, _jsonSerializerOptions);
                yield return $"data: {chunk}\n\n";
            }
            else
            {
                foreach (var token in tokens)
                {
                    chunk = JsonSerializer.Serialize(new ChatCompletionChunkResponse
                    {
                        id = id,
                        created = created,
                        model = request.model,
                        choices =
                        [
                            new ChatCompletionChunkResponseChoice
                            {
                                index = ++index,
                                delta = new ChatCompletionMessage
                                {
                                    role = null,
                                    content = token
                                },
                                finish_reason = null
                            }
                        ]
                    }, _jsonSerializerOptions);
                    yield return $"data: {chunk}\n\n";
                }
            }

            // 结束
            chunk = JsonSerializer.Serialize(new ChatCompletionChunkResponse
            {
                id = id,
                created = created,
                model = request.model,
                choices =
                [
                    new ChatCompletionChunkResponseChoice
                    {
                        index = tools.Count > 0 ? 0 : ++index,
                        delta = null,
                        finish_reason = tools.Count > 0 ? "tool_calls" : "stop"
                    }
                ]
            }, _jsonSerializerOptions);
            yield return $"data: {chunk}\n\n";
            yield return "data: [DONE]\n\n";
            yield break;
        }

        // 结束
        chunk = JsonSerializer.Serialize(new ChatCompletionChunkResponse
        {
            id = id,
            created = created,
            model = request.model,
            choices =
            [
                new ChatCompletionChunkResponseChoice
                {
                    index = ++index,
                    delta = null,
                    finish_reason = "stop"
                }
            ]
        }, _jsonSerializerOptions);
        yield return $"data: {chunk}\n\n";
        yield return "data: [DONE]\n\n";
        yield break;
    }

    /// 生成对话历史
    private ChatHistoryResult GetChatHistory(ChatCompletionRequest request)
    {
        // 生成工具提示
        var toolPrompt =
            _toolPromptGenerator.GenerateToolPrompt(request, _usedset.ToolPrompt.Index, _usedset.ToolPrompt.Lang);
        var toolenabled = !string.IsNullOrWhiteSpace(toolPrompt);
        var toolstopwords = toolenabled ? _toolPromptGenerator.GetToolStopWords(_usedset.ToolPrompt.Index) : null;

        var messages = request.messages;

        // 添加默认配置的系统提示
        if (!toolenabled && !string.IsNullOrWhiteSpace(_usedset.SystemPrompt) && messages.First()?.role != "system")
        {
            _logger.Debug("Add system prompt.");
            messages = messages.Prepend(new ChatCompletionMessage
            {
                role = "system",
                content = _usedset.SystemPrompt
            }).ToArray();
        }
        else if (toolenabled && messages.First()?.role != "system")
        {
            _logger.Debug("Add system prompt.");
            messages = messages.Prepend(new ChatCompletionMessage
            {
                role = "system",
                content = _usedset.SystemPrompt
            }).ToArray();
        }


        // 使用对话模版
        var history = "";
        if (_usedset.WithTransform?.HistoryTransform != null)
        {
            var type = Type.GetType(_usedset.WithTransform.HistoryTransform);
            if (type != null)
            {
                var historyTransform = Activator.CreateInstance(type) as ITransform;
                if (historyTransform != null)
                {
                    history = historyTransform.HistoryToText(messages, _toolPromptGenerator, _usedset.ToolPrompt,
                        toolPrompt);
                }
                else
                {
                    history = new BaseHistoryTransform().HistoryToText(messages, _toolPromptGenerator,
                        _usedset.ToolPrompt,
                        toolPrompt);
                }
            }
            else
            {
                history = new BaseHistoryTransform().HistoryToText(messages, _toolPromptGenerator, _usedset.ToolPrompt,
                    toolPrompt);
            }
        }
        else
        {
            history = new BaseHistoryTransform().HistoryToText(messages, _toolPromptGenerator, _usedset.ToolPrompt,
                toolPrompt);
        }

        return new ChatHistoryResult(history, toolenabled, toolstopwords);
    }

    #endregion

    #region Embedding

    public async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request,
        CancellationToken cancellationToken)
    {
        var embeddings = new List<float[]>();

        if (request.input is null || request.input.Length == 0)
        {
            _logger.Warning("No input.");
            return new EmbeddingResponse();
        }

        /*if (!LlmModelParams.ToModelParams(_usedset.LlmModelParams).Embeddings)
        {
            _logger.Warning("Model does not support embeddings.");
            return new EmbeddingResponse();
        }*/

        if (_embedder == null)
        {
            _logger.Warning("Embedder is null.");
            return new EmbeddingResponse();
        }

        foreach (var text in request.input)
        {
            var embedding = await _embedder.GetEmbeddings(text, cancellationToken);
            embeddings.AddRange(embedding);
        }

        return new EmbeddingResponse
        {
            data = embeddings.Select((x, index) => new EmbeddingObject
            {
                embedding = x,
                index = index
            }).ToArray(),
            model = request.model
        };
    }

    /// 是否支持嵌入
    // public bool IsSupportEmbedding => LlmModelParams.ToModelParams(_usedset.LlmModelParams).Embeddings;
    public bool IsSupportEmbedding => _embedder != null;

    #endregion

    #region Completion

    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.prompt))
        {
            _logger.Warning("No prompt.");
            return new CompletionResponse();
        }

        var genParams = GetInferenceParams(request, null);
        var ex = new MyStatelessExecutor(_model, LlmModelParams.ToModelParams(_usedset.LlmModelParams));
        var result = new StringBuilder();

        var completion_tokens = 0;
        await foreach (var output in ex.InferAsync(request.prompt, genParams, cancellationToken))
        {
            _logger.Debug("Message: {output}", output);
            result.Append(output);
            completion_tokens++;
        }

        var prompt_tokens = ex.PromptTokens;

        return new CompletionResponse
        {
            id = $"cmpl-{Guid.NewGuid():N}",
            model = request.model,
            created = DateTimeOffset.Now.ToUnixTimeSeconds(),
            choices = new[]
            {
                new CompletionResponseChoice
                {
                    index = 0,
                    text = result.ToString()
                }
            },
            usage = new UsageInfo
            {
                prompt_tokens = prompt_tokens,
                completion_tokens = completion_tokens,
                total_tokens = prompt_tokens + completion_tokens
            }
        };
    }

    public async IAsyncEnumerable<string> CreateCompletionStreamAsync(CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.prompt))
        {
            _logger.Warning("No prompt.");
            yield break;
        }

        var genParams = GetInferenceParams(request, null);
        var ex = new MyStatelessExecutor(_model, LlmModelParams.ToModelParams(_usedset.LlmModelParams));
        var id = $"cmpl-{Guid.NewGuid():N}";
        var created = DateTimeOffset.Now.ToUnixTimeSeconds();
        int index = 0;
        var chunk = JsonSerializer.Serialize(new CompletionResponse
        {
            id = id,
            created = created,
            model = request.model,
            choices = new[]
            {
                new CompletionResponseChoice
                {
                    index = index,
                    text = "",
                    finish_reason = null
                }
            }
        }, _jsonSerializerOptions);
        yield return $"data: {chunk}\n\n";
        await foreach (var output in ex.InferAsync(request.prompt, genParams, cancellationToken))
        {
            _logger.Debug("Message: {output}", output);
            chunk = JsonSerializer.Serialize(new CompletionResponse
            {
                id = id,
                created = created,
                model = request.model,
                choices = new[]
                {
                    new CompletionResponseChoice
                    {
                        index = ++index,
                        text = output,
                        finish_reason = null
                    }
                }
            }, _jsonSerializerOptions);
            yield return $"data: {chunk}\n\n";
        }

        chunk = JsonSerializer.Serialize(new CompletionResponse
        {
            id = id,
            created = created,
            model = request.model,
            choices = new[]
            {
                new CompletionResponseChoice
                {
                    index = ++index,
                    text = null,
                    finish_reason = "stop"
                }
            }
        }, _jsonSerializerOptions);
        yield return $"data: {chunk}\n\n";
        yield return "data: [DONE]\n\n";
        yield break;
    }

    #endregion

    /// 生成推理参数
    private InferenceParams GetInferenceParams(BaseCompletionRequest request, string[]? toolstopwords)
    {
        var stop = new List<string>();
        if (request.stop != null)
        {
            stop.AddRange(request.stop);
        }

        if (_usedset.AntiPrompts?.Length > 0)
        {
            stop.AddRange(_usedset.AntiPrompts);
        }

        if (toolstopwords?.Length > 0)
        {
            stop.AddRange(toolstopwords);
        }

        if (stop.Count > 0)
        {
            //去重，去空，并且至多4个
            stop = stop.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).Take(4).ToList();
        }

        InferenceParams inferenceParams = new InferenceParams()
        {
            MaxTokens = request.max_tokens.HasValue && request.max_tokens.Value > 0 ? request.max_tokens.Value : Int32.MaxValue,
            AntiPrompts = stop,
            Temperature = request.temperature,
            TopP = request.top_p,
            PresencePenalty = request.presence_penalty,
            FrequencyPenalty = request.frequency_penalty,
        };
        inferenceParams.SamplingPipeline = new DefaultSamplingPipeline();
        return inferenceParams;
    }


    #region Dispose

    /// 主动释放模型资源
    public void DisposeModel()
    {
        if (GlobalConfig.Instance.IsModelLoaded)
        {
            _embedder?.Dispose();
            _model.Dispose();
            GlobalConfig.Instance.IsModelLoaded = false;
            _loadModelIndex = -1;
        }
    }

    /// 是否已释放资源
    private bool _disposedValue = false;

    /// 释放非托管资源
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                DisposeModel();
            }

            _disposedValue = true;
        }
    }

    /// 释放资源
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}