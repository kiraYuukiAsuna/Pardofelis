﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PardofelisUI.ControlsLibrary.Dialog;
using SukiUI.Controls;
using PardofelisCore.Config;
using System.IO;

namespace PardofelisUI.Pages.VoiceOutputConfig;

public partial class VoiceOutputConfigPageViewModel : PageBase
{
    private string TTSConfigPath;

    [ObservableProperty]
    private DynamicUIConfig _dynamicUIConfig;

    public VoiceOutputConfigPageViewModel() : base("TTS语音输出配置", MaterialIconKind.FileCog, int.MinValue)
    {
        TTSConfigPath = Path.Join(CommonConfig.ConfigRootPath, "ApplicationConfig/VoiceOutputConfig.json");

        ReloadConfig();
    }

    [ObservableProperty] private int _id;
    [ObservableProperty] private string _lang;
    [ObservableProperty] private double _length;
    [ObservableProperty] private double _noise;
    [ObservableProperty] private double _noisew;
    [ObservableProperty] private double _sdpRatio;
    [ObservableProperty] private int _segmentSize;

    [RelayCommand]
    private void ReloadConfig()
    {
        PardofelisCore.Config.VoiceOutputConfig ttsConfig = PardofelisCore.Config.VoiceOutputConfig.ReadConfig(TTSConfigPath);

        Id = ttsConfig.Id;
        Length = ttsConfig.Length;
        Noise = ttsConfig.Noise;
        Noisew = ttsConfig.Noisew;
        SdpRatio = ttsConfig.SdpRatio;
        SegmentSize = ttsConfig.SegmentSize;
    }

    [RelayCommand]
    private void SaveConfig()
    {
        PardofelisCore.Config.VoiceOutputConfig ttsConfig = new PardofelisCore.Config.VoiceOutputConfig
        {
            Id = Id,
            Length = Length,
            Noise = Noise,
            Noisew = Noisew,
            SdpRatio = SdpRatio,
            SegmentSize = SegmentSize
        };

        PardofelisCore.Config.VoiceOutputConfig.WriteConfig(TTSConfigPath, ttsConfig);
        SukiHost.ShowDialog(new StandardDialog("保存配置文件成功!", "确定"));
    }
}