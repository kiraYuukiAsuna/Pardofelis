using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;
using CommunityToolkit.Mvvm.ComponentModel;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Serilog;
using Formatting = Newtonsoft.Json.Formatting;

namespace WeatherOneDayPlugin;

[JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
public partial class Config : ObservableObject
{
    public static string CurrentPluginWorkingDirectory = System.IO.Directory.GetCurrentDirectory();
    public static string CurrentPardofelisAppDataPath = CurrentPluginWorkingDirectory;
    public static ILogger CurLogger;
    
    public void Init()
    {
        var logFileFolder = Path.Join(Config.CurrentPardofelisAppDataPath, "PluginLog", ThisAssembly.AssemblyName);
        if (!Directory.Exists(logFileFolder))
        {
            Directory.CreateDirectory(logFileFolder);
        }
        
        CurLogger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Join(logFileFolder, "Log_" +  ThisAssembly.AssemblyName  + ".txt"), rollingInterval: RollingInterval.Day)
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

public class WeatherOneDayPlugin
{
    public Config PluginConfig = new Config();
    
    public WeatherOneDayPlugin()
    {
        PluginConfig.Init();
        PluginConfig = Config.ReadConfig();
    }
    
    [KernelFunction]
    [Description("查询今天的天气")]
    public async Task<string> GetTodayTemperature(
        Kernel kernel,
        [Description("请输入一个城市名称")] string inputCity
        )
    {
        string filePath = Path.Join(Config.CurrentPluginWorkingDirectory, "City.txt");

        // 读取文件内容到字典中
        var cityUrlMap = new Dictionary<string, string>();

        foreach (var line in File.ReadLines(filePath))
        {
            var parts = line.Split(',');
            if (parts.Length == 2)
            {
                string city = parts[1].Trim();
                string url = parts[0].Trim();
                cityUrlMap[city] = url;
            }
        }
        // 查找城市对应的网址
        if (cityUrlMap.TryGetValue(inputCity, out string cityUrl))
        {
            Config.CurLogger.Information($"城市: {inputCity} 对应的网址是: {cityUrl}");

            // 进入爬虫部分
            string weatherData = ScrapeWeatherData(cityUrl);

            // 返回爬取到的天气数据
            return weatherData;
        }
        else
        {
            Config.CurLogger.Information("未找到对应的城市。");
            return "未找到对应的城市。";

        }
    }

    // 爬虫方法，接受网址并进行页面抓取
    public string ScrapeWeatherData(string url)
    {
        var httpClient = new HttpClient();

        // 添加 User-Agent 头
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

        // 使用 Result 将异步请求转换为同步
        var html = httpClient.GetStringAsync(url).Result;  // 这行代码强制等待异步任务完成

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        var xpath0 = "/html/body/div[7]/div[1]/ul[1]/li[1]/p[2]";
        var node0 = htmlDocument.DocumentNode.SelectSingleNode(xpath0);
        string date = node0?.InnerText.Trim() ?? "日期信息未找到";

        var xpath1 = "/html/body/div[7]/div[1]/ul[1]/li[1]/p[3]";
        var node1 = htmlDocument.DocumentNode.SelectSingleNode(xpath1);
        string weather = node1?.InnerText.Trim() ?? "天气信息未找到";

        var xpath2 = "/html/body/div[7]/div[1]/ul[1]/li[1]/p[4]";
        var node2 = htmlDocument.DocumentNode.SelectSingleNode(xpath2);
        string temperature = node2?.InnerText.Trim() ?? "温度信息未找到";

        // 爬取建议1、2、3
        var advise1 = "/html/body/div[7]/div[1]/ul[2]/li[1]/div[2]/p";
        var adv1 = htmlDocument.DocumentNode.SelectSingleNode(advise1);
        string suggestion1 = adv1?.InnerText.Trim() ?? "建议1信息未找到";

        var advise2 = "/html/body/div[7]/div[1]/ul[2]/li[6]/div[2]/p";
        var adv2 = htmlDocument.DocumentNode.SelectSingleNode(advise2);
        string suggestion2 = adv2?.InnerText.Trim() ?? "建议2信息未找到";

        var advise3 = "/html/body/div[7]/div[1]/ul[2]/li[8]/div[2]/p";
        var adv3 = htmlDocument.DocumentNode.SelectSingleNode(advise3);
        string suggestion3 = adv3?.InnerText.Trim() ?? "建议3信息未找到";

        string result = $"日期: {date}, 天气: {weather}, 温度: {temperature}, " +
                        $"建议1: {suggestion1}, 建议2: {suggestion2}, 建议3: {suggestion3}";

        return result;
    }
}

