using Newtonsoft.Json;
using PardofelisCore.Util;
using Serilog;

namespace PardofelisCore.Config;

public class VoiceInputConfig
{
    public bool IsPressKeyReceiveAudioInputEnabled = true;
    public Keys HotKey = Keys.Tab;
    
    public static VoiceInputConfig ReadConfig(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            Log.Information("Config file not found. Creating a new one.");
            var newConfig = new VoiceInputConfig();
            var json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
            File.WriteAllText(configFilePath, json);
            return newConfig;
        }

        var config = JsonConvert.DeserializeObject<VoiceInputConfig>(File.ReadAllText(configFilePath));
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Read config info: {@ConfigManager}", config);

        if (config != null)
        {
            return config;
        }
        else
        {
            Log.Error("Failed to read config file.");
            return new VoiceInputConfig();
        }
    }

    public static void WriteConfig(string configFilePath, VoiceInputConfig configuration)
    {
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(configuration, Formatting.Indented));
        Log.Information("Write config info to file: {@ConfigManager}", configuration);
    }
}