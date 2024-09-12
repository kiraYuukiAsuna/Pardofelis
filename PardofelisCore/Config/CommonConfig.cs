namespace PardofelisCore.Config;

public static class CommonConfig
{
    public static string CurrentWorkingDirectory = System.IO.Directory.GetCurrentDirectory();
    public static string CurrentUserHomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string PardofelisAppDataPath = Path.Join(CurrentUserHomeDirectory, "PardofelisAppData");
    public static string ConfigRootPath = Path.Join(PardofelisAppDataPath, "Config");
    public static string ModelRootPath = Path.Join(PardofelisAppDataPath, "LlmModel");
    public static string LogRootPath = Path.Join(PardofelisAppDataPath, "Log");
    public static string EmbeddingModelRootPath = Path.Join(PardofelisAppDataPath, "LlmModel");
    public static string PluginRootPath = Path.Join(PardofelisAppDataPath, "Plugin");
    public static string PythonRootPath = Path.Join(PardofelisAppDataPath, "Python3.9.13");
    public static string VoiceModelRootPath = Path.Join(PardofelisAppDataPath, "VoiceModel");
    public static string FunctionCallPluginRootPath = Path.Join(PardofelisAppDataPath, "FunctionCallPlugin");
    public static string MemoryRootPath = Path.Join(PardofelisAppDataPath, "Memory");


    public static string GlobalConfigConfigPath = Path.Join(ConfigRootPath, "GlobalConfig.Instance.json");
    public static string LlmModelConfigConfigPath = Path.Join(ConfigRootPath, "LlmModelConfig.json");
    public static string ToolPromptConfigConfigPath = Path.Join(ConfigRootPath, "ToolPromptConfig.json");

}
