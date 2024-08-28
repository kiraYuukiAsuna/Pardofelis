using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PardofelisCore.Config;
using PardofelisCore.LlmController.LlamaSharpWrapper.Service;
using PardofelisCore.LlmController.OpenAiModel;
using Serilog;

namespace PardofelisCore.LlmController.LlamaSharpWrapper.ApiController;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly Serilog.ILogger _logger;

    public ChatController()
    {
        _logger = new LoggerConfiguration().WriteTo.Console()
            .WriteTo.File(Path.Join(CommonConfig.LogRootPath, "LogChatController.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    /// 默认不开启流式，需要主动设置 stream:true
    [HttpPost("/v1/chat/completions")]
    [HttpPost("/chat/completions")]
    [HttpPost("/openai/deployments/{model}/chat/completions")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ChatCompletionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IResult> CreateChatCompletionAsync([FromBody] ChatCompletionRequest request,
        [FromServices] ILlmModelService service, CancellationToken cancellationToken)
    {
        try
        {
            if (request.stream)
            {
                string first = " ";
                await foreach (var item in service.CreateChatCompletionStreamAsync(request, cancellationToken))
                {
                    if (first == " ")
                    {
                        first = item;
                    }
                    else
                    {
                        if (first.Length > 1)
                        {
                            Response.Headers.ContentType = "text/event-stream";
                            Response.Headers.CacheControl = "no-cache";
                            await Response.Body.FlushAsync();
                            await Response.WriteAsync(first);
                            await Response.Body.FlushAsync();
                            first = "";
                        }

                        await Response.WriteAsync(item);
                        await Response.Body.FlushAsync();
                    }
                }

                return Results.Empty;
            }
            else
            {
                return Results.Ok(await service.CreateChatCompletionAsync(request, cancellationToken));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in CreateChatCompletionAsync");
            return Results.Problem($"{ex.Message}");
        }
    }
}