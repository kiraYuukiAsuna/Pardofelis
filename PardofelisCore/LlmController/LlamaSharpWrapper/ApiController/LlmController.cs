using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PardofelisCore.Config;
using PardofelisCore.LlmController.LlamaSharpWrapper.Service;
using Serilog;


namespace PardofelisCore.LlmController.LlamaSharpWrapper.ApiController;

[Route("[controller]")]
[ApiController]
public class LLMController : ControllerBase
{
    private readonly Serilog.ILogger _logger;

    private readonly LlmModelConfigList _settings;

    public LLMController()
    {
        _settings = new LlmModelConfigList();
        Log.Information("LLMController init");
        _logger = new LoggerConfiguration().WriteTo.Console()
            .WriteTo.File(Path.Join(CommonConfig.LogRootPath, "LogLLMController.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        LlmModelConfigList config = LlmModelConfigList.ReadConfig();
        _settings.Models = config.Models;
    }

    /// 返回模型的基本信息
    [HttpGet("/models/info")]
    public JsonResult GetModels([FromServices] ILlmModelService service)
    {
        var json = service.GetModelInfo();
        return new JsonResult(json);
    }

    /// 返回已配置的模型信息
    [HttpGet("/models/config")]
    public ConfigModels GetConfigModels()
    {
        return new ConfigModels
        {
            Models = _settings.Models,
            Loaded = GlobalConfig.Instance.IsModelLoaded,
            Current = GlobalConfig.Instance.CurrentModelIndex
        };
    }

    /// 切换到指定模型
    [HttpPut("/models/{modelId}/switch")]
    public IActionResult SwitchModel(int modelId)
    {
        if (modelId < 0 || modelId >= _settings.Models.Count)
        {
            return BadRequest("Invalid model id");
        }

        // 保存当前模型索引
        int index = GlobalConfig.Instance.CurrentModelIndex;

        if (GlobalConfig.Instance.CurrentModelIndex == modelId)
        {
            return Ok();
        }

        try
        {
            GlobalConfig.Instance.CurrentModelIndex = modelId;
            var service = HttpContext.RequestServices.GetRequiredService<ILlmModelService>();
            service.InitModelIndex();
        }
        catch (Exception e)
        {
            GlobalConfig.Instance.CurrentModelIndex = index;
            return BadRequest(e.Message);
        }

        return Ok();
    }
}