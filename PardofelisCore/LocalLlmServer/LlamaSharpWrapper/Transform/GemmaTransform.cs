namespace PardofelisCore.LlmController.LlamaSharpWrapper.Transform;

public class GemmaHistoryTransform : BaseHistoryTransform
{
    protected override string userToken => "<start_of_turn>user";

    protected override string assistantToken => "<start_of_turn>model";

    protected override string systemToken => "";

    protected override string endToken => "<end_of_turn>";
}