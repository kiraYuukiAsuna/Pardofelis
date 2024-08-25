namespace PardofelisCore.Config;

public static class CommonConfig
{
    public static string CurrentWorkingDirectory = System.IO.Directory.GetCurrentDirectory();
    
    public static string PardofelisAppDataPath = Path.Join(CurrentWorkingDirectory, "PardofelisAppData");
    public static string LogRootPath = Path.Join(PardofelisAppDataPath, "Log");
    public static string ModelRootPath = Path.Join(PardofelisAppDataPath, "Model");
    public static string ConfigRootPath = Path.Join(PardofelisAppDataPath, "Config");


    public static string GlobalConfigConfigPath = Path.Join(ConfigRootPath, "GlobalConfig.Instance.json");
    public static string LlmModelConfigConfigPath = Path.Join(ConfigRootPath, "LlmModelConfig.json");
    public static string ToolPromptConfigConfigPath = Path.Join(ConfigRootPath, "ToolPromptConfig.json");

}
