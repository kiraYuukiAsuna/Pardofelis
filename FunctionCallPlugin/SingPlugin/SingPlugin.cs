using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Serilog;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using CdnDownload;

namespace SingPlugin;

public class ManSuiMusicPresetItem
{
    public string Name;
}

public class ManSuiMusicPreset
{
    public List<ManSuiMusicPresetItem> SongProviders;
}

[JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
public partial class Config : ObservableObject
{
    public static string CurrentPluginWorkingDirectory = System.IO.Directory.GetCurrentDirectory();
    public static string CurrentPardofelisAppDataPath = CurrentPluginWorkingDirectory;
    public static ILogger CurLogger;

    [ObservableProperty] [JsonProperty("SongDirectory")]
    public string _songDirectory = Path.Join(CurrentPardofelisAppDataPath, "Music");

    [ObservableProperty] [JsonProperty("LocalMode")]
    public bool _localMode = false;

    [ObservableProperty] [JsonProperty("SongProviders")]
    public List<KeyValuePair<bool, string>> _songProviders = new();

    [ObservableProperty] [JsonProperty("WebBrowserPath")]
    public string _webBrowserPath = "";

    private void UserInit()
    {
        try
        {
            var downloadPath = Path.Join(CurrentPardofelisAppDataPath, "Download", "Resources/ManSuiMusic");
            var fileName = "ManSuiMusicPreset.json";

            Dl123Pan.DownloadByPathAndName("/directlink/Resources/ManSuiMusic", fileName, downloadPath, fileName);

            var preset =
                JsonConvert.DeserializeObject<ManSuiMusicPreset>(File.ReadAllText(Path.Join(downloadPath, fileName)));

            var config = ReadConfig();

            List<KeyValuePair<bool, string>> newProviders = new();

            foreach (var provider in preset.SongProviders)
            {
                bool bFind = false;
                foreach (var local in config.SongProviders)
                {
                    if (local.Value == provider.Name)
                    {
                        bFind = true;
                        newProviders.Add(new KeyValuePair<bool, string>(local.Key, provider.Name));
                        break;
                    }
                }

                if (!bFind)
                {
                    newProviders.Add(new KeyValuePair<bool, string>(false, provider.Name));
                }
            }

            config.SongProviders = newProviders;
            WriteConfig(config);

            foreach (var provider in newProviders)
            {
                if (provider.Key)
                {
                    var name = provider.Value + ".txt";
                    try
                    {
                        Dl123Pan.DownloadByPathAndName("/directlink/Resources/ManSuiMusic", name, downloadPath, name);
                    }
                    catch (Exception e)
                    {
                        CurLogger.Error(e, "UserInit error");
                    }
                }
            }
        }
        catch (Exception e)
        {
            CurLogger.Error(e, "UserInit error");
        }
    }

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

        UserInit();
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

public class SingPlugin
{
    public Config PluginConfig = new Config();

    // 存储歌曲名称和路径的字典
    Dictionary<string, string> Songs = new Dictionary<string, string>();
    static SoundPlayer player = new SoundPlayer(); // 使用System.Media.SoundPlayer来播放.wav文件
    static bool isPlaying = false; // 追踪是否正在播放
    static string currentSongName = ""; // 记录当前播放的歌曲名称
    private Process ProcessInfo = new Process();


    public SingPlugin()
    {
        PluginConfig.Init();
        PluginConfig = Config.ReadConfig();

        // 歌曲文件夹路径
        string folderPath = PluginConfig.SongDirectory; // 替换为实际路径
        if (Directory.Exists(folderPath))
        {
            if (PluginConfig.LocalMode)
            {
                Config.CurLogger.Information("歌曲文件夹路径：{folderPath}", folderPath);
                // 读取文件夹中所有.wav文件
                string[] files = Directory.GetFiles(folderPath, "*.wav");

                foreach (var file in files)
                {
                    // 获取文件名（不包括路径）
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                    Songs[fileNameWithoutExtension] = file; // 将歌曲名称和对应的文件路径存入字典
                }
            }
            else
            {
                var downloadPath = Path.Join(Config.CurrentPardofelisAppDataPath, "Download", "Resources/ManSuiMusic");

                foreach (var provider in PluginConfig.SongProviders)
                {
                    if (provider.Key)
                    {
                        var name = provider.Value + ".txt";
                        var musicList = Path.Join(downloadPath, name);

                        string[] lines = File.ReadAllLines(musicList);
                        foreach (var line in lines)
                        {
                            var sp = line.Split("&&");
                            if (sp.Length == 2)
                            {
                                Songs[sp[1]] = sp[0];
                            }
                        }
                    }
                }
            }
        }
    }

    [KernelFunction]
    [Description("开始唱歌，切换歌曲")]
    public async Task<string> StartSingAsync(
        Kernel kernel,
        [Description("请输入歌曲名称")] string songName
    )
    {
        if (PluginConfig.LocalMode)
        {
            bool bFind = false;
            foreach (var song in Songs)
            {
                if (song.Key.Contains(songName))
                {
                    bFind = true;

                    // 如果有歌曲正在播放，先停止
                    if (isPlaying)
                    {
                        StopSong();
                    }

                    // 播放新歌曲
                    string songPath = song.Value;
                    PlaySong(songPath);
                    currentSongName = songName;
                    Config.CurLogger.Information("歌曲开始播放：{songName}", songName);
                    return "歌曲开始播放了~";
                }
            }

            if (!bFind)
            {
                Config.CurLogger.Information("未找到该歌曲：{songName}", songName);
                var message = "未找到该歌曲。推荐你几首歌曲：";
                Random random = new Random();
                List<string> songList = Songs.Keys.ToList(); // 将键转换为列表
                int count = 0;
                while (count < 10 && songList.Count > 0)
                {
                    // 从剩下的歌曲中随机挑选一个
                    int index = random.Next(songList.Count);
                    message += songList[index];
                    songList.RemoveAt(index); // 移除已选中的歌曲
                    count++;

                    if (count < 10 && songList.Count > 0)
                    {
                        message += "， "; // 如果不是最后一首歌，添加逗号
                    }
                }

                return message += "你想选择那首";
            }

            return "歌曲开始播放了~";
        }
        else
        {
            var bvId = "";
            foreach (var song in Songs)
            {
                if (song.Key.Contains(songName))
                {
                    bvId = song.Value;
                    break;
                }
            }

            if (bvId == "")
            {
                Config.CurLogger.Information("未找到该歌曲：{songName}", songName);
                var message = "未找到该歌曲。推荐你几首歌曲：";
                Random random = new Random();
                List<string> songList = Songs.Keys.ToList(); // 将键转换为列表
                int count = 0;
                while (count < 10 && songList.Count > 0)
                {
                    // 从剩下的歌曲中随机挑选一个
                    int index = random.Next(songList.Count);
                    message += songList[index];
                    songList.RemoveAt(index); // 移除已选中的歌曲
                    count++;

                    if (count < 10 && songList.Count > 0)
                    {
                        message += "， "; // 如果不是最后一首歌，添加逗号
                    }
                }


                return message += "你想选择那首";
            }

            try
            {
                string url = $"https://www.bilibili.com/video/{bvId}/";
                string arguments = "--new-window " + url;
                ProcessInfo = Process.Start(new ProcessStartInfo
                {
                    FileName = PluginConfig.WebBrowserPath,
                    Arguments = arguments
                });
            }
            catch (Exception e)
            {
                Config.CurLogger.Error(e, "StartSingAsync error");
                return "打开浏览器播放在线歌曲失败~错误信息是：" + e.Message;
            }

            currentSongName = songName;

            return "歌曲开始播放了~";
        }
    }


    [KernelFunction]
    [Description("停止唱歌")]
    public async Task<string> StopSingAsync(
        Kernel kernel)
    {
        if (PluginConfig.LocalMode)
        {
            if (isPlaying)
            {
                StopSong();
                Config.CurLogger.Information("歌曲停止播放：{currentSongName}", currentSongName);
                return "歌曲停止播放了~";
            }
            else
            {
                Config.CurLogger.Information("当前没有正在播放的歌曲。");
                return "当前没有正在播放的歌曲。";
            }
        }
        else
        {
            return "歌曲停止播放了~";
        }
    }

    // 播放歌曲的方法
    static void PlaySong(string songPath)
    {
        player.SoundLocation = songPath;
        player.Load(); // 加载音频文件
        player.Play(); // 播放音频文件
        isPlaying = true;
        Config.CurLogger.Information($"正在播放：{currentSongName}");
    }

    // 停止歌曲的方法
    static void StopSong()
    {
        if (isPlaying)
        {
            player.Stop(); // 停止播放
            isPlaying = false;
            Config.CurLogger.Information($"已停止播放：{currentSongName}");
            currentSongName = ""; // 清空当前播放的歌曲名称
        }
        else
        {
            Config.CurLogger.Information("当前没有正在播放的歌曲。");
        }
    }
}