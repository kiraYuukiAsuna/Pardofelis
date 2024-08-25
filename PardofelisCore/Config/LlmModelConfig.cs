using LLama.Common;
using Newtonsoft.Json;
using Serilog;

namespace PardofelisCore.Config;

/// 模型配置信息
public class LlmModelConfig
{
    /// 模型名称
    public string Name { get; set; } = "default";

    /// 模型描述
    public string? Description { get; set; }

    /// 模型站点介绍
    public string? WebSite { get; set; }

    /// 模型版本
    public string? Version { get; set; }

    /// 对话时未指定系统提示词时使用的默认配置
    public string? SystemPrompt { get; set; }

    /// 模型加载参数
    public LlmModelParams LlmModelParams { get; set; } = new();

    /// 模型转换参数
    public WithTransform? WithTransform { get; set; }

    /// 停止词
    public string[]? AntiPrompts { get; set; }

    /// 工具提示配置
    public ToolPromptInfo ToolPrompt { get; set; } = new();
}

public class LlmModelConfigList
{
    public List<LlmModelConfig> Models { get; set; } = new();

    /// Embedding模型
    public string EmbeddingModelFileName { get; set; } = "";
    
    [JsonIgnore]
    private static string ConfigFilePath = Path.Join(CommonConfig.ConfigRootPath, "LlmModelConfig.json");

    public static LlmModelConfigList ReadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            Log.Information("Config file {0} not found. Create a new one.", ConfigFilePath);
            var newConfig = new LlmModelConfigList();
            var json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
            return newConfig;
        }

        var config = JsonConvert.DeserializeObject<LlmModelConfigList>(File.ReadAllText(ConfigFilePath));
        File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Read config {0} info: {1}", ConfigFilePath, config);
        return config;
    }

    public static void WriteConfig(LlmModelConfigList config)
    {
        File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Write config {0} info to file: {1}", ConfigFilePath, config);
    }
}

/// 模型转换参数
public class WithTransform
{
    /// 对话转换
    public string? HistoryTransform { get; set; }

    /// 输出转换
    public string? OutputTransform { get; set; }
}

/// 工具提示配置
public class ToolPromptInfo
{
    /// 提示模板索引
    public int Index { get; set; } = 0;

    /// 模板提示语言
    public string Lang { get; set; } = "en";
}

/// 模型配置信息
public class ConfigModels
{
    /// 当前使用的模型
    public int Current { get; set; }

    /// 是否已加载
    public bool Loaded { get; set; }

    /// 模型列表
    public List<LlmModelConfig>? Models { get; set; }
}

public class LlmModelParams
{
    public uint ContextSize { get; set; } = 2048;
    public int MainGpu { get; set; } = 0;
    public int GpuLayerCount { get; set; } = 29;
    public uint Seed { get; set; } = 1111;
    public bool UseMemoryMap { get; set; } = true;
    public bool UseMemoryLock { get; set; } = true;
    public string ModelFileName { get; set; } = "";
    public uint Threads { get; set; } = 8;
    public uint BatchThreads { get; set; } = 8;
    public uint BatchSize { get; set; } = 512u;
    public uint UBatchSize { get; set; } = 512u;

    public static ModelParams ToModelParams(LlmModelParams llmModelParams)
    {
        var modelParams = new ModelParams(Path.Join(CommonConfig.ModelRootPath,llmModelParams.ModelFileName));
        modelParams.ContextSize = llmModelParams.ContextSize;
        modelParams.MainGpu = llmModelParams.MainGpu;
        modelParams.GpuLayerCount = llmModelParams.GpuLayerCount;
        modelParams.Seed = llmModelParams.Seed;
        modelParams.UseMemorymap = llmModelParams.UseMemoryMap;
        modelParams.UseMemoryLock = llmModelParams.UseMemoryLock;
        modelParams.Threads = llmModelParams.Threads;
        modelParams.BatchThreads = llmModelParams.BatchThreads;
        modelParams.BatchSize = llmModelParams.BatchSize;
        modelParams.UBatchSize = llmModelParams.UBatchSize;
        return modelParams;
    }

    public static LlmModelParams ToLlmModelParams(ModelParams modelParams)
    {
        var llmModelParams = new LlmModelParams();
        llmModelParams.ContextSize = modelParams.ContextSize ?? 2048;
        llmModelParams.MainGpu = modelParams.MainGpu;
        llmModelParams.GpuLayerCount = modelParams.GpuLayerCount;
        llmModelParams.Seed = modelParams.Seed ?? 1111;
        llmModelParams.UseMemoryMap = modelParams.UseMemorymap;
        llmModelParams.UseMemoryLock = modelParams.UseMemoryLock;
        llmModelParams.ModelFileName = Path.GetFileName(modelParams.ModelPath);
        llmModelParams.Threads = modelParams.Threads ?? 8;
        llmModelParams.BatchThreads = modelParams.BatchThreads ?? 8;
        llmModelParams.BatchSize = modelParams.BatchSize;
        llmModelParams.UBatchSize = modelParams.UBatchSize;
        return llmModelParams;
    }
}