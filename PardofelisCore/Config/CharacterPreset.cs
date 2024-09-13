using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PardofelisCore.Config;

public struct CharacterPreset
{
    public string Name { get; set; }

    public string YourName { get; set; }

    public ChatContent ChatContent { get; set; }
    public List<string> ExceptTextRegexExpression { get; set; }
    public List<string> EnabledPlugins { get; set; }
    public string HotZhWords { get; set; }
    public string HotRules { get; set; }

    public int IdleAskMeTime { get; set; }
    public string IdleAskMeMessage { get; set; }

    public CharacterPreset()
    {
        Name = "";
        YourName = "";
        ChatContent = new ChatContent();
        ExceptTextRegexExpression = new List<string>();
        EnabledPlugins = new List<string>();
        HotZhWords = "";
        HotRules = "";
        IdleAskMeTime = -1;
        IdleAskMeMessage = "";
    }

    public CharacterPreset(string name, string yourName, ChatContent chatContent,
        List<string> exceptTextRegexExpression,
        List<string> enabledPlugins, string hotZhWords, string hotRules, string idleAskMeMessage, int idleAskMeTime)
    {
        Name = name;
        YourName = yourName;
        ChatContent = chatContent;
        ExceptTextRegexExpression = exceptTextRegexExpression;
        EnabledPlugins = enabledPlugins;
        HotZhWords = hotZhWords;
        HotRules = hotRules;
        IdleAskMeTime = idleAskMeTime;
        IdleAskMeMessage = idleAskMeMessage;
    }

    public static CharacterPreset ReadConfig(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            Log.Information("Config file not found. Creating a new one.");
            var newConfig = new CharacterPreset();
            var json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
            File.WriteAllText(configFilePath, json);
            return newConfig;
        }

        var config = JsonConvert.DeserializeObject<CharacterPreset>(File.ReadAllText(configFilePath));
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Read config info: {@ConfigManager}", config);

        return config;
    }

    public static void WriteConfig(string configFilePath, CharacterPreset modelParameterConfig)
    {
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(modelParameterConfig, Formatting.Indented));
        Log.Information("Write config info to file: {@ConfigManager}", modelParameterConfig);
    }

    public static List<string> GetAllConfigFileNames(string configRootPath)
    {
        if (Directory.Exists(configRootPath) == false)
        {
            Directory.CreateDirectory(configRootPath);
        }

        var files = Directory.GetFiles(configRootPath);
        List<string> configFileNames = new();
        foreach (var file in files)
        {
            if (Path.GetExtension(file) == ".json")
            {
                try
                {
                    CharacterPreset.ReadConfig(file);
                    configFileNames.Add(Path.GetRelativePath(configRootPath, file));
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                }
            }
        }

        return configFileNames;
    }
}
