using System.Text;
using System.Text.Json;
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
public class EmbeddingController : ControllerBase
{
    private readonly Serilog.ILogger _logger;
    private readonly ILlmModelService _modelService;
    private readonly HttpClient _client;


    public EmbeddingController(ILlmModelService modelService, HttpClient client)
    {
        _logger = new LoggerConfiguration().WriteTo.Console()
            .WriteTo.File(Path.Join(CommonConfig.LogRootPath, "LogEmbeddingController.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        _modelService = modelService;
        _client = client;
    }

    [HttpPost("/v1/embeddings")]
    [HttpPost("/embeddings")]
    [HttpPost("/openai/deployments/{model}/embeddings")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmbeddingResponse))]
    public async Task<IResult> CreateEmbeddingAsync([FromBody] EmbeddingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request == null)
            {
                return Results.BadRequest("Request is null");
            }

            // 使用模型服务创建嵌入
            if (_modelService.IsSupportEmbedding)
            {
                var response = await _modelService.CreateEmbeddingAsync(request, cancellationToken);
                return Results.Ok(response);
            }
            else
            {
                // 转发请求
                var url = "http://127.0.0.1:5000/embeddings";

                if (string.IsNullOrEmpty(url))
                {
                    return Results.BadRequest("EmbedingForward is null");
                }

                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(result);
                    return Results.Ok(embeddingResponse);
                }
                else
                {
                    return Results.BadRequest(response.ReasonPhrase);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in CreateEmbeddingAsync");
            return Results.Problem($"{ex.Message}");
        }
    }
}