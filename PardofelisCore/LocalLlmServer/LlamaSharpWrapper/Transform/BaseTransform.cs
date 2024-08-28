using System.Text;
using PardofelisCore.Config;
using PardofelisCore.LlmController.LlamaSharpWrapper.FunctionCall;
using PardofelisCore.LlmController.OpenAiModel;

namespace PardofelisCore.LlmController.LlamaSharpWrapper.Transform;

public class BaseHistoryTransform : ITransform
{
    /// 用户标记
    protected virtual string userToken => "<|im_start|>user";

    /// 助理标记
    protected virtual string assistantToken => "<|im_start|>assistant";

    /// 系统标记
    protected virtual string systemToken => "<|im_start|>system";

    /// 结束标记
    protected virtual string endToken => "<|im_end|>";

    /// 记录 function 的调用信息
    private Dictionary<string, string> functionCalls = new Dictionary<string, string>();

    /// 历史记录转换为文本
    public virtual string HistoryToText(ChatCompletionMessage[] history, ToolPromptGenerator generator,
        ToolPromptInfo toolinfo, string toolPrompt = "")
    {
        // 若有系统消息，则会放在最开始
        // 用于处理模型不支持系统消息角色设定的情况
        var systemMessage = "";

        StringBuilder sb = new();

        // 是否等待工具调用
        bool toolWait = false;
        // 防止重复添加系统消息
        bool systemAdd = false;
        foreach (var message in history)
        {
            if (message.role == "user")
            {
                if (toolWait)
                {
                    // 此处处理异常，工具激活，但是后面没有工具调用反馈
                    functionCalls.Clear();
                    toolWait = false;
                    sb.AppendLine(endToken);
                }

                sb.AppendLine($"{userToken}\n{systemMessage}{message.content}{endToken}");
                systemMessage = "";
            }
            else if (message.role == "system")
            {
                if (systemAdd || toolWait) continue;
                systemAdd = true;
                // 模型不支持系统消息角色设定
                if (string.IsNullOrWhiteSpace(systemToken))
                {
                    systemMessage = $"{message.content} {toolPrompt}";
                }
                else
                {
                    sb.AppendLine($"{systemToken}\n{message.content}{toolPrompt}{endToken}");
                }
            }
            else if (message.role == "assistant")
            {
                // 工具调用后的助理消息
                if (toolWait)
                {
                    // 保存工具调用结果
                    if (functionCalls.Count > 0)
                    {
                        foreach (var call in functionCalls)
                        {
                            sb.AppendLine(call.Value);
                        }

                        functionCalls.Clear();
                    }

                    var toolCallReturn = generator.GenerateToolCallReturn(message.content, toolinfo.Index);
                    sb.AppendLine($"{toolCallReturn}{endToken}");
                    toolWait = false;
                }
                else
                {
                    // 存在工具调用
                    if (message.tool_calls?.Length > 0)
                    {
                        sb.AppendLine($"{assistantToken}");
                        foreach (var toolCall in message.tool_calls)
                        {
                            var toolCallPrompt = generator.GenerateToolCall(toolCall, toolinfo.Index);
                            sb.AppendLine($"{toolCallPrompt}");
                            // 创建占位，等待工具调用结果
                            functionCalls.Add(toolCall.id, "");
                        }

                        var toolSplit = generator.GetToolResultSplit(toolinfo.Index);
                        sb.Append($"{toolSplit}");
                        toolWait = true;
                    }
                    else
                    {
                        sb.AppendLine($"{assistantToken}\n{message.content}{endToken}");
                    }
                }
            }
            else if (message.role == "tool")
            {
                // 异常情况，不应该出现
                if (message.tool_call_id is null || !toolWait || functionCalls.Count == 0 ||
                    !functionCalls.ContainsKey(message.tool_call_id)) continue;
                var toolCallResult = generator.GenerateToolCallResult(message.content, toolinfo.Index);
                // 保存工具调用结果
                functionCalls[message.tool_call_id] = toolCallResult;
            }
        }

        // 增加推理提示符
        var lastMessage = history.LastOrDefault();
        if (lastMessage?.role == "tool" && functionCalls.Count > 0)
        {
            // 添加工具调用结果
            foreach (var call in functionCalls)
            {
                sb.AppendLine(call.Value);
            }

            functionCalls.Clear();
            // 添加工具推理提示符
            sb.AppendLine(generator.GetToolPromptConfig(toolinfo.Index).FN_EXIT);
        }
        else if (toolWait)
        {
            // 异常情况，说明最后一条消息是工具调用，激活了工具，但是没有工具调用结果
            // 结束工具调用，再次提示助理推理
            sb.AppendLine($"{endToken}{assistantToken}");
        }
        else
        {
            // 一般情况，添加助理提示符
            sb.AppendLine(assistantToken);
        }

        //Console.WriteLine(sb.ToString());
        return sb.ToString();
    }
}