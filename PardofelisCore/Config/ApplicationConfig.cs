using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PardofelisCore.Config;

public enum ModelType
{
    Local,
    Online
}

public enum TextInputMode
{
    Text,
    Voice,
}

public struct ApplicationConfig
{
    public string PardofelisAppDataPrefixPath;

    public string LastSelectedModelConfigFileName;
    public string LastSelectedCharacterPresetConfigFileName;

    public ModelType LastSelectedModelType;
    public TextInputMode LastSelectedTextInputMode;

    public string LastSelectedModelFileName;

    public ApplicationConfig()
    {
        PardofelisAppDataPrefixPath = "";
        LastSelectedModelConfigFileName = "";
        LastSelectedCharacterPresetConfigFileName = "";
        LastSelectedModelType = ModelType.Online;
        LastSelectedTextInputMode = TextInputMode.Text;
        LastSelectedModelFileName = "";
    }

    public ApplicationConfig(string pardofelisAppDataPrefixPath, string lastSelectedModelConfigFileName, string lastSelectedCharacterPresetConfigFileName,
        ModelType modelType, TextInputMode textInputMode, string lastSelectedModelFileName)
    {
        PardofelisAppDataPrefixPath = pardofelisAppDataPrefixPath;
        LastSelectedModelConfigFileName = lastSelectedModelConfigFileName;
        LastSelectedCharacterPresetConfigFileName = lastSelectedCharacterPresetConfigFileName;
        LastSelectedModelType = modelType;
        LastSelectedTextInputMode = textInputMode;
        LastSelectedModelFileName = lastSelectedModelFileName;
    }


    public static ApplicationConfig ReadConfig(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            Log.Information("Config file not found. Creating a new one.");
            var newConfig = new ApplicationConfig();
            var json = JsonConvert.SerializeObject(newConfig,Formatting.Indented);
            File.WriteAllText(configFilePath, json);
            return newConfig;
        }

        var config = JsonConvert.DeserializeObject<ApplicationConfig>(File.ReadAllText(configFilePath));
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Read config info: {@ConfigManager}", config);

        return config;
    }

    public static void WriteConfig(string configFilePath, ApplicationConfig configuration)
    {
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(configuration, Formatting.Indented));
        Log.Information("Write config info to file: {@ConfigManager}", configuration);
    }
}
