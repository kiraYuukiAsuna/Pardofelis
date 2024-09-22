using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Serilog;
using System.ComponentModel;
using System.Media;
using System.Text.RegularExpressions;

namespace SingPlugin;

[JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
public partial class Config : ObservableObject
{
    public static string CurrentPluginWorkingDirectory = System.IO.Directory.GetCurrentDirectory();
    public static string CurrentPardofelisAppDataPath = CurrentPluginWorkingDirectory;
    public static ILogger CurLogger;
    
    [ObservableProperty]
    [JsonProperty("SongDirectory")]
    public string _songDirectory = Path.Join(CurrentPardofelisAppDataPath, "Music");

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

public class SingPlugin
{
    public Config PluginConfig = new Config();

    // 存储歌曲名称和路径的字典
    Dictionary<string, string> Songs = new Dictionary<string, string>();
    static SoundPlayer player = new SoundPlayer();  // 使用System.Media.SoundPlayer来播放.wav文件
    static bool isPlaying = false;  // 追踪是否正在播放
    static string currentSongName = "";  // 记录当前播放的歌曲名称

    public SingPlugin()
    {
        PluginConfig.Init();
        PluginConfig = Config.ReadConfig();

        // 歌曲文件夹路径
        string folderPath = PluginConfig.SongDirectory;  // 替换为实际路径

        if(Directory.Exists(folderPath))
        {
            Config.CurLogger.Information("歌曲文件夹路径：{folderPath}", folderPath);
            // 读取文件夹中所有.wav文件
            string[] files = Directory.GetFiles(folderPath, "*.wav");

            // 正则表达式提取《》内的内容
            Regex regex = new Regex(@"《(.*?)》");

            foreach (var file in files)
            {
                // 获取文件名（不包括路径）
                string fileName = Path.GetFileName(file);

                // 使用正则表达式提取歌曲名称
                Match match = regex.Match(fileName);
                if (match.Success)
                {
                    string songName = match.Groups[1].Value;
                    Songs[songName] = file;  // 将歌曲名称和对应的文件路径存入字典
                }
            }
        }
    }

    ~SingPlugin()
    {
        StopSong();
    }

    [KernelFunction]
    [Description("开始唱歌，切换歌曲")]
    public async Task<string> StartSingAsync(
        Kernel kernel,
        [Description("请输入歌曲名称")] string songName
        )
    {
        // 如果输入的是歌曲名称，检查是否存在该歌曲
        if (Songs.ContainsKey(songName))
        {
            // 如果有歌曲正在播放，先停止
            if (isPlaying)
            {
                StopSong();
            }

            // 播放新歌曲
            string songPath = Songs[songName];
            PlaySong(songPath);
            currentSongName = songName;
            Config.CurLogger.Information("歌曲开始播放：{songName}", songName);
            return "歌曲开始播放捏~";
        }
        else
        {
            Config.CurLogger.Information("未找到该歌曲：{songName}", songName);
            var message = "未找到该歌曲。已有歌曲列表为：";
            foreach (var song in Songs.Keys)
            {
                message += song + "、";
            }
            return message;
        }
    }


    [KernelFunction]
    [Description("停止唱歌")]
    public async Task<string> StopSingAsync(
        Kernel kernel)
    {
        if (isPlaying)
        {
            StopSong();
            Config.CurLogger.Information("歌曲停止播放：{currentSongName}", currentSongName);
            return "歌曲停止播放了捏~";
        }
        else
        {
            Config.CurLogger.Information("当前没有正在播放的歌曲。");
            return "当前没有正在播放的歌曲。";
        }
    }

    // 播放歌曲的方法
    static void PlaySong(string songPath)
    {
        player.SoundLocation = songPath;
        player.Load();  // 加载音频文件
        player.Play();  // 播放音频文件
        isPlaying = true;
        Config.CurLogger.Information($"正在播放：{currentSongName}");
    }

    // 停止歌曲的方法
    static void StopSong()
    {
        if (isPlaying)
        {
            player.Stop();  // 停止播放
            isPlaying = false;
            Config.CurLogger.Information($"已停止播放：{currentSongName}");
            currentSongName = "";  // 清空当前播放的歌曲名称
        }
        else
        {
            Config.CurLogger.Information("当前没有正在播放的歌曲。");
        }
    }

}
