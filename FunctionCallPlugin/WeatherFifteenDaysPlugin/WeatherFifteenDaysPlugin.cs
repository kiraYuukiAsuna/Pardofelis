using Microsoft.SemanticKernel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Serilog;

namespace WeatherFifteenDaysPlugin;

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
        var configFilePath = Path.Join(CurrentPluginWorkingDirectory, "Config.json");
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        var json = JsonConvert.SerializeObject(config, settings);
        File.WriteAllText(configFilePath, json);
        Config.CurLogger.Information("Write config info to file: {@ConfigManager}", config);
    }
}
public class WeatherFifteenDaysPlugin
{
    public Config PluginConfig = new Config();

    WeatherFifteenDaysPlugin()
    {
        PluginConfig.Init();
        PluginConfig = Config.ReadConfig();
    }
    
    [KernelFunction]
    [Description("查询最近15天的天气，当对话中出现未来的规划时，需要考虑天气")]
    public async Task<string> GetCurrentTemperature(
        Kernel kernel,
        [Description("请输入一个城市名称")] string inputCity
        )
    {
        string filePath =Path.Join(Config.CurrentPluginWorkingDirectory, "City.txt");

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
        string result = "";

        for (int i = 1; i <= 22; i++)
        {
            // 构建li的XPath
            var liXPath = $"/html/body/div[6]/div[3]/ul[1]/li[{i}]";

            // 获取li的内容
            var liNode = htmlDocument.DocumentNode.SelectSingleNode(liXPath);
            if (liNode != null)
            {
                // 获取各个部分的内容
                var dateXPath = $"{liXPath}/a/div[1]/span[1]";
                var todayXPath = $"{liXPath}/a/div[1]/span[2]";
                var weatherXPath = $"{liXPath}/a/div[3]";
                var temperatureXPath = $"{liXPath}/a/div[4]";

                var dateNode = htmlDocument.DocumentNode.SelectSingleNode(dateXPath);
                var todayNode = htmlDocument.DocumentNode.SelectSingleNode(todayXPath);
                var weatherNode = htmlDocument.DocumentNode.SelectSingleNode(weatherXPath);
                var temperatureNode = htmlDocument.DocumentNode.SelectSingleNode(temperatureXPath);

                // 打印内容
                string date = dateNode?.InnerText.Trim() ?? "建议2信息未找到";
                string today = todayNode?.InnerText.Trim() ?? "建议2信息未找到";
                string weather = weatherNode?.InnerText.Trim() ?? "建议2信息未找到";
                string temperature = temperatureNode?.InnerText.Trim() ?? "建议2信息未找到";
                result =result+"\n"+"日期:"+date+"天气:" +weather+"温度: "+temperature;
            }
            else
            {
                Config.CurLogger.Information($"未找到li[{i}]元素。");
            }
        }
        return result;
    }
}

