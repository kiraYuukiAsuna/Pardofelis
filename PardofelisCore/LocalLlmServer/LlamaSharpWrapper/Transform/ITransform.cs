using PardofelisCore.Config;
using PardofelisCore.LlmController.LlamaSharpWrapper.FunctionCall;
using PardofelisCore.LlmController.OpenAiModel;

namespace PardofelisCore.LlmController.LlamaSharpWrapper.Transform;

public interface ITransform
{
    public string HistoryToText(ChatCompletionMessage[] history, ToolPromptGenerator generator, ToolPromptInfo toolinfo,
        string toolPrompt);
}