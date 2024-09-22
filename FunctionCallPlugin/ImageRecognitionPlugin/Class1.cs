using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using Serilog;

namespace ImageRecognitionPlugin;

[JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
public partial class Config : ObservableObject
{
    public static string CurrentPluginWorkingDirectory = System.IO.Directory.GetCurrentDirectory();
    public static string CurrentPardofelisAppDataPath = CurrentPluginWorkingDirectory;
    public static ILogger CurLogger;

    [ObservableProperty] [JsonProperty("ModelName")]
    public string _modelName = "gpt-4o-mini";

    [ObservableProperty] [JsonProperty("Url")]
    public string _url = "";

    [ObservableProperty] [JsonProperty("ApiKey")]
    public string _apiKey = "";


    public void Init()
    {
        var logFileFolder = Path.Join(Config.CurrentPardofelisAppDataPath, "PluginLog", ThisAssembly.AssemblyName);
        if (!Directory.Exists(logFileFolder))
        {
            Directory.CreateDirectory(logFileFolder);
        }

        CurLogger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Join(logFileFolder, "Log_" + ThisAssembly.AssemblyName + ".txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public static Config ReadConfig()
    {
        var pluginConfigFolder = Path.Join(CurrentPardofelisAppDataPath, "PluginConfig", ThisAssembly.AssemblyName);
        var configFilePath = Path.Join(pluginConfigFolder, "Config.json");
        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            Config.CurLogger.Information("Config file not found. Creating a new one.");
            var newConfig = new Config();
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            var json = JsonConvert.SerializeObject(newConfig, settings);
            File.WriteAllText(configFilePath, json);
            return newConfig;
        }

        var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFilePath));
        Config.CurLogger.Information("Read config info: {@ConfigManager}", config);

        return config != null ? config : new Config();
    }

    public static void WriteConfig(Config config)
    {
        var pluginConfigFolder = Path.Join(CurrentPardofelisAppDataPath, "PluginConfig", ThisAssembly.AssemblyName);
        var configFilePath = Path.Join(pluginConfigFolder, "Config.json");
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        var json = JsonConvert.SerializeObject(config, settings);
        File.WriteAllText(configFilePath, json);
        Config.CurLogger.Information("Write config info to file: {@ConfigManager}", config);
    }
}

public class ImageRecognitionPlugin
{
    public Config PluginConfig = new Config();

    private Kernel Kernel;
    private IChatCompletionService ChatCompletionService;
    
    public ImageRecognitionPlugin()
    {
        PluginConfig.Init();
        PluginConfig = Config.ReadConfig();

        try
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(PluginConfig.ModelName,
                PluginConfig.ApiKey, "", "", new HttpClient()
                {
                    BaseAddress = new Uri(PluginConfig.Url),
                    Timeout = TimeSpan.FromMinutes(3)
                });
            Kernel = builder.Build();
            ChatCompletionService = Kernel.GetRequiredService<IChatCompletionService>();
        }catch(Exception e)
        {
            Config.CurLogger.Error("Error when creating kernel: " + e.Message);
        }
    }

    [KernelFunction]
    [Description("识别图像内容，在需要知道用户正在做什么的时候，可以通过识别图像内容（目前是屏幕截图方式）来获取上下文信息。")]
    public async Task<string> ImageRecognitionAsync(
        Kernel kernel
    )
    {
        Config.CurLogger.Information("开始识别图像内容...");
        var result = GetImageDescription();
        Config.CurLogger.Information("识别图像内容完成！");
        return result;
    }


    // 定义Windows API函数
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc,
        int nXSrc, int nYSrc, int dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    const int SRCCOPY = 0x00CC0020;

    public static ReadOnlyMemory<byte> ConvertBitmapToReadOnlyMemory(Bitmap bitmap)
    {
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

        using (MemoryStream memoryStream = new MemoryStream())
        {
            // 将Bitmap对象保存到内存流中
            bitmap.Save(memoryStream, ImageFormat.Png);

            // 获取字节数组
            byte[] byteArray = memoryStream.ToArray();

            // 将字节数组转换为ReadOnlyMemory<byte>
            return new ReadOnlyMemory<byte>(byteArray);
        }
    }

    public string GetImageDescription()
    {
        try
        {
            // 获取屏幕的尺寸
            int screenWidth = GetSystemMetrics(0); // SM_CXSCREEN
            int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN

            IntPtr hWnd = IntPtr.Zero; // 代表整个屏幕
            IntPtr hScreenDC = GetDC(hWnd);
            IntPtr hMemoryDC = CreateCompatibleDC(hScreenDC);

            IntPtr hBitmap = CreateCompatibleBitmap(hScreenDC, screenWidth, screenHeight);
            IntPtr hOldBitmap = SelectObject(hMemoryDC, hBitmap);

            BitBlt(hMemoryDC, 0, 0, screenWidth, screenHeight, hScreenDC, 0, 0, SRCCOPY);

            Bitmap bmp = Image.FromHbitmap(hBitmap);
            
            // bmp.Save("screenshot.png", ImageFormat.Png);
            // Config.CurLogger.Information("Screenshot saved as screenshot.png");

            // 清理资源
            SelectObject(hMemoryDC, hOldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(hMemoryDC);
            ReleaseDC(hWnd, hScreenDC);

            ChatHistory chatMessages = new ChatHistory();
            var systemPrompt = "识别并分析图像内容，给出详细的描述信息，以便能够作为上下文供AI助手进行回答。";

            chatMessages.AddMessage(AuthorRole.User, new ChatMessageContentItemCollection()
            {
                new TextContent(systemPrompt),
                new ImageContent(ConvertBitmapToReadOnlyMemory(bmp), mimeType: "image/png")
            });

            if (string.IsNullOrEmpty(PluginConfig.ModelName))
            {
                return "识别图像内容失败！请先配置支持图像的在线大模型名称！";
            }

            if (string.IsNullOrEmpty(PluginConfig.Url))
            {
                return "识别图像内容失败！请先配置支持图像的在线大模型请求地址！";
            }

            if (string.IsNullOrEmpty(PluginConfig.ApiKey))
            {
                return "识别图像内容失败！请先配置支持图像的在线大模型密钥！";
            }
            
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                ChatSystemPrompt = systemPrompt,
                Temperature = 0.0f
            };
            var result = ChatCompletionService.GetChatMessageContentsAsync(
                chatMessages,
                executionSettings: openAIPromptExecutionSettings,
                kernel: Kernel);

            Config.CurLogger.Information("Assistant > ");
            var assistantMessage = result.Result.FirstOrDefault()?.Content;
            Config.CurLogger.Information(assistantMessage);

            chatMessages.AddAssistantMessage(assistantMessage);

            return "识别图像内容成功！图像识别结果：\n" + assistantMessage;
        }
        catch (Exception e)
        {
            return "识别图像内容失败！错误信息:\n" + e.Message;
        }
    }
}