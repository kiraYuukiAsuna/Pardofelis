namespace PardofelisCore.LlmController.LlamaSharpWrapper.Transform;

public class ZephyrHistoryTransform : BaseHistoryTransform
{
    protected override string userToken => "<|user|>";

    protected override string assistantToken => "<|assistant|>";

    protected override string systemToken => "<|system|>";

    protected override string endToken => "<|end|>";
}