using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PardofelisCore.Config;
using System.IO;
using System.Linq;
using Avalonia.Collections;
using SukiUI.Dialogs;

namespace PardofelisUI.Pages.VoiceOutputConfig;

public partial class VoiceOutputConfigPageViewModel : PageBase
{
    private string TTSConfigPath;

    [ObservableProperty] private DynamicUIConfig _dynamicUIConfig;

    public VoiceOutputConfigPageViewModel() : base("TTS语音输出配置", MaterialIconKind.FileCog, int.MinValue)
    {
        TTSConfigPath = Path.Join(CommonConfig.ConfigRootPath, "ApplicationConfig/VoiceOutputConfig.json");

        ReloadConfig();
    }

    [ObservableProperty] private int _id;
    [ObservableProperty] private AvaloniaList<string> _ttsModelNameList=new();
    [ObservableProperty] private string _currentTTSModelName;
    [ObservableProperty] private string _lang;
    [ObservableProperty] private double _length;
    [ObservableProperty] private double _noise;
    [ObservableProperty] private double _noisew;
    [ObservableProperty] private double _sdpRatio;
    [ObservableProperty] private int _segmentSize;

    [RelayCommand]
    private void ReloadConfig()
    {
        TtsModelNameList.Clear();
        CurrentTTSModelName = "";
        
        TTSConfigPath = Path.Join(CommonConfig.ConfigRootPath, "ApplicationConfig/VoiceOutputConfig.json");

        PardofelisCore.Config.VoiceOutputConfig ttsConfig =
            PardofelisCore.Config.VoiceOutputConfig.ReadConfig(TTSConfigPath);

        if (!Path.Exists(Path.Join(CommonConfig.VoiceModelRootPath, "VoiceOutput", "onnx")))
        {
            return;
        }
        
        string modelPath = Path.Join(CommonConfig.VoiceModelRootPath, "VoiceOutput", "onnx");
        var modelFolders = Directory.GetDirectories(modelPath);

        foreach (var modelFolder in modelFolders)
        {
            var ttsModelName = Path.GetFileName(modelFolder);
            if (File.Exists(Path.Join(modelFolder, ttsModelName + "_dec.onnx")) &&
                File.Exists(Path.Join(modelFolder, ttsModelName + "_dp.onnx")) &&
                File.Exists(Path.Join(modelFolder, ttsModelName + "_emb.onnx")) &&
                File.Exists(Path.Join(modelFolder, ttsModelName + "_enc_p.onnx")) &&
                File.Exists(Path.Join(modelFolder, ttsModelName + "_flow.onnx")) &&
                File.Exists(Path.Join(modelFolder, ttsModelName + "_sdp.onnx")))
            {
                TtsModelNameList.Add(ttsModelName);
            }
        }

        if (TtsModelNameList.Contains(ttsConfig.TTSModelName))
        {
            CurrentTTSModelName = ttsConfig.TTSModelName;
        }
        else
        {
            CurrentTTSModelName = TtsModelNameList.First();
            ttsConfig.TTSModelName = TtsModelNameList.First();
            PardofelisCore.Config.VoiceOutputConfig.WriteConfig(TTSConfigPath, ttsConfig);
        }

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
            TTSModelName = CurrentTTSModelName,
            Length = Length,
            Noise = Noise,
            Noisew = Noisew,
            SdpRatio = SdpRatio,
            SegmentSize = SegmentSize
        };

        PardofelisCore.Config.VoiceOutputConfig.WriteConfig(TTSConfigPath, ttsConfig);

        DynamicUIConfig.GlobalDialogManager.CreateDialog()
            .WithTitle("提示！")
            .WithContent("保存配置文件成功!")
            .WithActionButton("确定", _ => { }, true)
            .TryShow();
    }
}