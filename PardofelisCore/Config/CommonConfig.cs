using Microsoft.Extensions.Configuration;
using PardofelisCore.Util;

namespace PardofelisCore.Config;

public static class CommonConfig
{
    public static string CurrentWorkingDirectory = System.IO.Directory.GetCurrentDirectory();
    public static ApplicationConfig PardofelisAppSettings = new ApplicationConfig();

    
    public static string ToolCallPluginRootPath = "";

    
    public static string PardofelisAppDataPath = "";
    public static string ConfigRootPath = "";
    public static string EmbeddingModelRootPath = "";
    public static string LocalLlmModelRootPath = "";
    public static string LogRootPath = CurrentWorkingDirectory;
    public static string MemoryRootPath = "";
    public static string PluginRootPath = "";
    public static string PythonRootPath = "";
    public static string VoiceModelRootPath = "";


    public static string GlobalConfigConfigPath="";
    public static string LlmModelConfigConfigPath = "";
    public static string ToolPromptConfigConfigPath = "";
}
