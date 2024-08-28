using System.Text.Json.Serialization;

namespace PardofelisCore.LlmController.OpenAiModel;

/// 参数信息，用于描述函数的参数
public class ParameterInfo
{
    /// 参数类型
    public required string type { get; set; }
    
    /// 参数的描述
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? description { get; set; }
    
    /// 参数的可选值
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? @enum { get; set; }
    
    /// 参数是否必需
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? required { get; set; }
}

/// 函数参数
public class Parameters
{
    /// 参数类型，当前只支持 object
    public string type { get; set; } = "object";
    
    /// 参数的属性，描述为JSON模式对象。
    /// 所含键值的类型为 ParameterInfo
    public required Dictionary<string, ParameterInfo> properties { get; set; }
    
    /// 必需的参数名称列表
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? required { get; set; }
}

/// 函数信息
public class FunctionInfo
{
    /// 要调用的函数的名称。必须是a-z、a-z、0-9，或包含下划线和短划线，最大长度为64。
    public required string name { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? description { get; set; }
    
    /// 函数接受的参数，描述为JSON模式对象。
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Parameters? parameters { get; set; }
}

public class ToolInfo
{
    /// 工具类型，当前只支持 function
    public string type { get; set; } = "function";
    
    public required FunctionInfo function { get; set; }
    
}

public class ToolMessage
{
    /// 工具调用的 ID
    public string id { get; set; }
    
    /// 工具类型，当前固定 function
    public string type { get; set; } = "function";
    
    /// 调用的函数信息
    public ToolMeaasgeFuntion function { get; set; }
}

/// 调用工具的响应选择
public class ToolMeaasgeFuntion
{
    /// 函数名称
    public string name { get; set; }
    
    /// 调用函数的参数，由JSON格式的模型生成。
    /// 请注意，该模型并不总是生成有效的JSON，并且可能会产生函数模式未定义的参数的幻觉。
    /// 在调用函数之前，验证代码中的参数。
    public string? arguments { get; set; }
}
