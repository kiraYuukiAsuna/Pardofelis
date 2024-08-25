using Newtonsoft.Json;
using Serilog;

namespace PardofelisCore.Config;

/// 全局配置
public class GlobalConfig
{
    ///  初始加载的模型索引
    public int CurrentModelIndex { get; set; } = 0;
    
    ///  模型是否完成了加载
    [JsonIgnore]
    public bool IsModelLoaded { get; set; } = false;

    /// 模型自动释放时间
    public int AutoReleaseTime { get; set; } = 0;

    [JsonIgnore]
    private static string ConfigFilePath = Path.Join(CommonConfig.ConfigRootPath, "GlobalConfig.json");

    public static GlobalConfig Instance;
    
    public static GlobalConfig ReadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            Log.Information("Config file {0} not found. Create a new one.", ConfigFilePath);
            var newConfig = new GlobalConfig();
            var json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
            return newConfig;
        }

        var config = JsonConvert.DeserializeObject<GlobalConfig>(File.ReadAllText(ConfigFilePath));
        File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Read config {0} info: {1}", ConfigFilePath, config);
        return config;
    }

    public static void WriteConfig(GlobalConfig config)
    {
        File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Write config {0} info to file: {1}", ConfigFilePath, config);
    }
}