namespace PluginBase;

[AttributeUsage(AttributeTargets.Class)]
public class ConfigurationAttribute : Attribute
{
    public string ConfigFilePath { get; }
    public bool RelativeToAppData { get; }

    public ConfigurationAttribute(string configFilePath, bool relativeToAppData)
    {
        ConfigFilePath = configFilePath;
        RelativeToAppData = relativeToAppData;
    }
}
