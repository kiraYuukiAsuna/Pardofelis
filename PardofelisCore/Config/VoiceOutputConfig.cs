using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PardofelisCore.Config;

public struct VoiceOutputConfig
{
    public int Id { get; set; }
    public string Format { get; set; }
    public string Lang { get; set; }
    public double Length { get; set; }
    public double Noise { get; set; }
    public double Noisew { get; set; }
    public double SdpRatio { get; set; }
    public int SegmentSize { get; set; }

    public VoiceOutputConfig()
    {
        Id = 0;
        Format = "wav";
        Lang = "zh";
        Length = 1.0;
        Noise = 0.33;
        Noisew = 0.4;
        SdpRatio = 0.2;
        SegmentSize = 50;
    }

    public VoiceOutputConfig(int id, string format, string lang, double length, double noise, double noisew,
        double sdpRatio, int segmentSize)
    {
        Id = id;
        Format = format;
        Lang = lang;
        Length = length;
        Noise = noise;
        Noisew = noisew;
        SdpRatio = sdpRatio;
        SegmentSize = segmentSize;
    }

    public static VoiceOutputConfig ReadConfig(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            Log.Information("Config file not found. Creating a new one.");
            var newConfig = new VoiceOutputConfig();
            var json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
            File.WriteAllText(configFilePath, json);
            return newConfig;
        }

        var config = JsonConvert.DeserializeObject<VoiceOutputConfig>(File.ReadAllText(configFilePath));
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        Log.Information("Read config info: {@ConfigManager}", config);

        return config;
    }

    public static void WriteConfig(string configFilePath, VoiceOutputConfig configuration)
    {
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(configuration, Formatting.Indented));
        Log.Information("Write config info to file: {@ConfigManager}", configuration);
    }
}
