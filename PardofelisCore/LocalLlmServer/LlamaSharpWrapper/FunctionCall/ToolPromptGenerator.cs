using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Microsoft.Extensions.Options;
using PardofelisCore.LlmController.OpenAiModel;

namespace PardofelisCore.LlmController.LlamaSharpWrapper.FunctionCall;

public class ToolPromptGenerator
{
    private readonly List<ToolPromptConfig> _config;

    private readonly string[] _nullWords = new string[] { "null", "{}", "[]" };

    /// 基础工具提示生成器
    public ToolPromptGenerator()
    {
        var config = ToolPromptConfigList.ReadConfig();
        _config = config.ToolPrompts;
    }

    /// 检查工具是否激活
    public bool IsToolActive(List<string> tokens, int tpl = 0)
    {
        return string.Join("", tokens).Trim().StartsWith(_config[tpl].FN_NAME);
    }

    /// 获取工具停用词
    public string[] GetToolStopWords(int tpl = 0)
    {
        return _config[tpl].FN_STOP_WORDS;
    }

    /// 获取工具结果分割符
    public string GetToolResultSplit(int tpl = 0)
    {
        return _config[tpl].FN_RESULT_SPLIT;
    }

    /// 获取工具提示配置
    public ToolPromptConfig GetToolPromptConfig(int tpl = 0)
    {
        return _config[tpl];
    }

    /// 生成工具调用
    public string GenerateToolCall(ToolMessage tool, int tpl = 0)
    {
        return string.Format(_config[tpl].FN_CALL_TEMPLATE, tool.function.name, tool.function.arguments);
    }

    /// 生成工具返回结果
    public string GenerateToolCallResult(string? res, int tpl = 0)
    {
        return string.Format(_config[tpl].FN_RESULT_TEMPLATE, res);
    }

    /// 生成工具推理结果
    public string GenerateToolCallReturn(string? res, int tpl = 0)
    {
        return $"{_config[tpl].FN_EXIT}: {res}";
    }

    /// 检查并生成工具调用
    public List<ToolMeaasgeFuntion> GenerateToolCall(string input, int tpl = 0)
    {
        string pattern = _config[tpl].FN_TEST;
        Regex regex = new Regex(pattern, RegexOptions.Singleline);
        MatchCollection matches = regex.Matches(input);
        List<ToolMeaasgeFuntion> results = new();
        foreach (Match match in matches)
        {
            string functionName = match.Groups[1].Value;
            string arguments = match.Groups[3].Success ? match.Groups[3].Value : "";
            if (string.IsNullOrWhiteSpace(arguments) || _nullWords.Contains(arguments))
            {
                arguments = null;
            }

            results.Add(new ToolMeaasgeFuntion
            {
                name = functionName,
                arguments = arguments,
            });
        }

        return results;
    }

    /// 生成工具提示词
    public string GenerateToolPrompt(ChatCompletionRequest req, int tpl = 0, string lang = "zh")
    {
        // 如果没有工具或者工具选择为 none，则返回空字符串
        if (req.tools == null || req.tools.Length == 0 ||
            (req.tool_choice != null && req.tool_choice.ToString() == "none"))
        {
            return string.Empty;
        }

        var config = _config[tpl];

        var toolDescriptions = req.tools
            .Select(tool => GetFunctionDescription(tool.function, config.ToolDescTemplate[lang])).ToArray();
        var toolNames = string.Join(",", req.tools.Select(tool => tool.function.name));

        var toolDescTemplate = config.FN_CALL_TEMPLATE_INFO[lang];
        var toolDesc = string.Join("\n\n", toolDescriptions);
        var toolSystem = toolDescTemplate.Replace("{tool_descs}", toolDesc);

        var parallelFunctionCalls = req.tool_choice?.ToString() == "parallel";
        var toolTemplate = parallelFunctionCalls
            ? config.FN_CALL_TEMPLATE_FMT_PARA[lang]
            : config.FN_CALL_TEMPLATE_FMT[lang];
        var toolPrompt = string.Format(toolTemplate, config.FN_NAME, config.FN_ARGS, config.FN_RESULT, config.FN_EXIT,
            toolNames);
        return $"\n\n{toolSystem}\n\n{toolPrompt}";
    }

    private string GetFunctionDescription(FunctionInfo function, string toolDescTemplate)
    {
        var nameForHuman = function.name;
        var nameForModel = function.name;
        var descriptionForModel = function.description ?? string.Empty;

        // 函数无参数
        if (function.parameters == null || function.parameters.properties == null ||
            function.parameters.properties.Count == 0)
        {
            return string.Format(toolDescTemplate, nameForHuman, nameForModel, descriptionForModel, string.Empty)
                .Trim();
        }

        //处理参数 required 字段
        var properties = function.parameters.properties;
        if (function.parameters.required?.Length > 0)
        {
            foreach (var key in function.parameters.required)
            {
                if (properties.ContainsKey(key))
                {
                    properties[key].required = true;
                }
            }
        }

        var parameters = JsonSerializer.Serialize(properties,
            new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });
        return string.Format(toolDescTemplate, nameForHuman, nameForModel, descriptionForModel, parameters).Trim();
    }
}