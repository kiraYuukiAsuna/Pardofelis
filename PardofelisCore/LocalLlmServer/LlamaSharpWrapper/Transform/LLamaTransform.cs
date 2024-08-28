namespace PardofelisCore.LlmController.LlamaSharpWrapper.Transform;

public class LLamaHistoryTransform : BaseHistoryTransform
{
    protected override string userToken => "<|start_header_id|>user<|end_header_id|>\n";

    protected override string assistantToken => "<|start_header_id|>assistant<|end_header_id|>\n";

    protected override string systemToken => "<|start_header_id|>system<|end_header_id|>\n";

    protected override string endToken => "<|eot_id|>";
}