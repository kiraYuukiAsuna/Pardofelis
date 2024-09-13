using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PardofelisCore.Config;

public class LocalLlmCreateInfo
{
    public UInt32 ContextSize;
    public float Temperature;
    public Int32 NGpuLayers;
    public UInt32 Seed;
    public bool UseMemoryLock;
    public UInt32 BatchThreads;
    public UInt32 Threads;
    public UInt32 BatchSize;
    public bool FlashAttention;

    public LocalLlmCreateInfo() : this(2048, 0.7f, 0, 114514, true, Convert.ToUInt32(Environment.ProcessorCount),
        Convert.ToUInt32(Environment.ProcessorCount), 512, true)
    {
    }

    public LocalLlmCreateInfo(UInt32 contextSize, float temperature, Int32 nGpuLayers, UInt32 seed, bool useMemoryLock,
        UInt32 batchThreads, UInt32 threads, UInt32 batchSize, bool flashAttention)
    {
        ContextSize = contextSize;
        Temperature = temperature;
        NGpuLayers = nGpuLayers;
        Seed = seed;
        UseMemoryLock = useMemoryLock;
        BatchThreads = batchThreads;
        Threads = threads;
        BatchSize = batchSize;
        FlashAttention = flashAttention;
    }
}

public class OnlineLlmCreateInfo
{
    public float Temperature;
    public int ContextSize;
    public string OnlineModelName;
    public string OnlineModelUrl;
    public string OnlineModelApiKey;

    public OnlineLlmCreateInfo() : this(0.7f, 2048, "", "", "")
    {
    }

    public OnlineLlmCreateInfo(float temperature, int contextSize, string onlineModelName, string onlineModelUrl,
        string onlineModelApiKey)
    {
        Temperature = temperature;
        ContextSize = contextSize;
        OnlineModelName = onlineModelName;
        OnlineModelUrl = onlineModelUrl;
        OnlineModelApiKey = onlineModelApiKey;
    }
}

public struct ModelParameterConfig
{
    public string Name { get; set; }
    public LocalLlmCreateInfo LocalLlmCreateInfo { get; set; }
    public ModelType ModelType { get; set; }
    public OnlineLlmCreateInfo OnlineLlmCreateInfo { get; set; }
    public TextInputMode TextInputMode { get; set; }

    public ModelParameterConfig()
    {
        Name = "默认名称";
        LocalLlmCreateInfo = new LocalLlmCreateInfo();
        ModelType = ModelType.Local;
        OnlineLlmCreateInfo = new OnlineLlmCreateInfo();
        TextInputMode = TextInputMode.Text;
    }

    public static ModelParameterConfig ReadConfig(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            Log.Information("Config file not found. Creating a new one.");
            var configManager = new ModelParameterConfig();
            var json = JsonConvert.SerializeObject(configManager, Formatting.Indented);
            File.WriteAllText(configFilePath, json);
            return configManager;
        }

        var config = JsonConvert.DeserializeObject<ModelParameterConfig>(File.ReadAllText(configFilePath));
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Read config info: {@ConfigManager}", config);

        return config;
    }

    public static void WriteConfig(string configFilePath, ModelParameterConfig modelParameterConfig)
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
                    ModelParameterConfig.ReadConfig(file);
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
