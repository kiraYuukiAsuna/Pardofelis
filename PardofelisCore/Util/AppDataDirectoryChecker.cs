

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PardofelisCore.Config;
using Serilog;

namespace PardofelisCore.Util;

public class AppDataDirectoryChecker
{
    private static string PardofelisAppSettingsFilePath = Path.Join(CommonConfig.CurrentWorkingDirectory, "PardofelisAppSettings.json");

    private static void ReloadConifg()
    {
        CommonConfig.ToolCallPluginRootPath = Path.Join(CommonConfig.CurrentWorkingDirectory, "ToolCallPlugin");
        
        CommonConfig.PardofelisAppDataPath = Path.Join(CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath, "PardofelisAppData");
        CommonConfig.ConfigRootPath = Path.Join(CommonConfig.PardofelisAppDataPath, "Config");
        CommonConfig.EmbeddingModelRootPath = Path.Join(CommonConfig.PardofelisAppDataPath, "EmbeddingModel");
        CommonConfig.LocalLlmModelRootPath = Path.Join(CommonConfig.PardofelisAppDataPath, "LocalLlmModel");
        CommonConfig.LogRootPath = Path.Join(CommonConfig.PardofelisAppDataPath, "Log");
        CommonConfig.MemoryRootPath = Path.Join(CommonConfig.PardofelisAppDataPath, "Memory_text-embedding-ada-002");
        CommonConfig.PluginRootPath = Path.Join(CommonConfig.PardofelisAppDataPath, "Plugin");
        CommonConfig.PythonRootPath = Path.Join(CommonConfig.PardofelisAppDataPath, "Python3.9.13");
        CommonConfig.VoiceModelRootPath = Path.Join(CommonConfig.PardofelisAppDataPath, "VoiceModel");


        CommonConfig.GlobalConfigConfigPath = Path.Join(CommonConfig.ConfigRootPath, "GlobalConfig.Instance.json");
        CommonConfig.LlmModelConfigConfigPath = Path.Join(CommonConfig.ConfigRootPath, "LlmModelConfig.json");
        CommonConfig.ToolPromptConfigConfigPath = Path.Join(CommonConfig.ConfigRootPath, "ToolPromptConfig.json");
    }

    public static void InitPardofelisAppSettings()
    {

        CommonConfig.PardofelisAppSettings = ApplicationConfig.ReadConfig(PardofelisAppSettingsFilePath);

        if(string.IsNullOrEmpty(CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath))
        {
            CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        ReloadConifg();
    }

    public static ResultWrap<string> GetCurrentPardofelisAppDataPrefixPath()
    {
        if (!string.IsNullOrEmpty(CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath))
        {
            return new ResultWrap<string>(true, CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath);
        }
        else
        {
            return new ResultWrap<string>(false, "");
        }
    }

    public static ResultWrap<string> SetCurrentPardofelisAppDataPrefixPath(string prefixPath)
    {
        if (!string.IsNullOrEmpty(prefixPath))
        {
            if (!Directory.Exists(prefixPath))
            {
                return new ResultWrap<string>(false, $"PrefixPath [{prefixPath}] not exist");
            }

            CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath = prefixPath;
            ApplicationConfig.WriteConfig(PardofelisAppSettingsFilePath, CommonConfig.PardofelisAppSettings);

            ReloadConifg();
            return new ResultWrap<string>(true, $"Set PardofelisAppDataPrefixPath to {prefixPath}");
        }
        else
        {
            return new ResultWrap<string>(false, "PrefixPath is null or empty");
        }
    }

    public static ResultWrap<string> CheckAppDataDirectoryAndCreateNoExist()
    {
        if (!Directory.Exists(CommonConfig.ToolCallPluginRootPath))
        {
            Log.Information($"ToolCallPluginRootPath [{CommonConfig.ToolCallPluginRootPath}] not exist");
            Directory.CreateDirectory(CommonConfig.ToolCallPluginRootPath);
        }
        
        if (!Directory.Exists(CommonConfig.PardofelisAppDataPath))
        {
            Log.Information($"PardofelisAppDataPath [{CommonConfig.PardofelisAppDataPath}] not exist");
            return new ResultWrap<string>(false, $"PardofelisAppDataPath [{CommonConfig.PardofelisAppDataPath}] not exist");
        }
        if (!Directory.Exists(CommonConfig.PythonRootPath))
        {
            return new ResultWrap<string>(false, $"PythonRootPath [{CommonConfig.PythonRootPath}] not exist");
        }
        if (!Directory.Exists(CommonConfig.VoiceModelRootPath))
        {
            return new ResultWrap<string>(false, $"VoiceModelRootPath [{CommonConfig.VoiceModelRootPath}] not exist");
        }
        if (!Directory.Exists(CommonConfig.ConfigRootPath))
        {
            Log.Information($"ConfigRootPath [{CommonConfig.ConfigRootPath}] not exist");
            Directory.CreateDirectory(CommonConfig.ConfigRootPath);
        }
        if (!Directory.Exists(CommonConfig.EmbeddingModelRootPath))
        {
            Log.Information($"EmbeddingModelRootPath [{CommonConfig.EmbeddingModelRootPath}] not exist");
            Directory.CreateDirectory(CommonConfig.EmbeddingModelRootPath);
        }
        if (!Directory.Exists(CommonConfig.LocalLlmModelRootPath))
        {
            Log.Information($"LocalLlmModelRootPath [{CommonConfig.LocalLlmModelRootPath}] not exist");
            Directory.CreateDirectory(CommonConfig.LocalLlmModelRootPath);
        }
        if (!Directory.Exists(CommonConfig.LogRootPath))
        {
            Log.Information($"LogRootPath [{CommonConfig.LogRootPath}] not exist");
            Directory.CreateDirectory(CommonConfig.LogRootPath);
        }
        if (!Directory.Exists(CommonConfig.MemoryRootPath))
        {
            Log.Information($"MemoryRootPath [{CommonConfig.MemoryRootPath}] not exist");
            Directory.CreateDirectory(CommonConfig.MemoryRootPath);
        }
        if (!Directory.Exists(CommonConfig.PluginRootPath))
        {
            Log.Information($"PluginRootPath [{CommonConfig.PluginRootPath}] not exist");
            Directory.CreateDirectory(CommonConfig.PluginRootPath);
        }
        return new ResultWrap<string>(true, "All AppData Directory Exist");
    }
}
