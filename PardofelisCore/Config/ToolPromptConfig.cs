using Newtonsoft.Json;
using PardofelisCore.Config;
using Serilog;

namespace PardofelisCore.LlmController.LlamaSharpWrapper.FunctionCall;

/// 工具提示配置
public class ToolPromptConfig
{
    // 工具提示配置的介绍信息
    public string PromptConfigDesc { get; set; }

    /// 工具名称占位符
    public string FN_NAME { get; set; }

    /// 工具参数占位符
    public string FN_ARGS { get; set; }

    /// 工具结果占位符
    public string FN_RESULT { get; set; }

    /// 函数调用模板
    public string FN_CALL_TEMPLATE { get; set; }

    /// 函数调用与结果分隔符
    public string FN_RESULT_SPLIT { get; set; }

    /// 工具结果模板
    public string FN_RESULT_TEMPLATE { get; set; }

    /// 函数调提取的正则表达式，用于提取函数名和参数
    public string FN_TEST { get; set; }

    /// 工具返回占位符
    public string FN_EXIT { get; set; }

    /// 工具停止词列表
    public string[] FN_STOP_WORDS { get; set; }

    /// 工具描述模板信息，按语言区分
    public Dictionary<string, string> FN_CALL_TEMPLATE_INFO { get; set; }

    /// 工具调用模板，按语言区分
    public Dictionary<string, string> FN_CALL_TEMPLATE_FMT { get; set; }

    /// 并行工具调用模板，按语言区分
    public Dictionary<string, string> FN_CALL_TEMPLATE_FMT_PARA { get; set; }

    /// 工具描述模板，按语言区分
    public Dictionary<string, string> ToolDescTemplate { get; set; }
}

public class ToolPromptConfigList
{
    public List<ToolPromptConfig> ToolPrompts { get; set; } = new();
    
    [JsonIgnore]
    private static string ConfigFilePath = Path.Join(CommonConfig.ConfigRootPath, "ToolPromptConfig.json");

    public static ToolPromptConfigList ReadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            Log.Information("Config file {0} not found. Create a new one.", ConfigFilePath);
            var newConfig = new ToolPromptConfigList();
            var json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
            return newConfig;
        }

        var config = JsonConvert.DeserializeObject<ToolPromptConfigList>(File.ReadAllText(ConfigFilePath));
        File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Read config {0} info: {1}", ConfigFilePath, config);
        return config;
    }

    public static void WriteConfig(ToolPromptConfigList config)
    {
        File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Write config {0} info to file: {1}", ConfigFilePath, config);
    }
}
