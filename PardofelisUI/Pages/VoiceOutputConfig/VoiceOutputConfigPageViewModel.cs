using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PardofelisCore.Config;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using PardofelisCore.Util;
using PardofelisCore.VoiceOutput;
using PardofelisUI.Utilities;
using Serilog;
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
    [ObservableProperty] private AvaloniaList<string> _ttsModelNameList = new();
    [ObservableProperty] private string _currentTTSModelName;
    [ObservableProperty] private string _lang;
    [ObservableProperty] private double _length;
    [ObservableProperty] private double _noise;
    [ObservableProperty] private double _noisew;
    [ObservableProperty] private double _sdpRatio;
    [ObservableProperty] private int _segmentSize;
    [ObservableProperty] private string _text;
    [ObservableProperty] private string _audioName;

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

    [RelayCommand]
    private void GenerateAudioFile()
    {
        if (string.IsNullOrEmpty(Text))
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("请输入文本!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        if (string.IsNullOrEmpty(AudioName))
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("请输入音频文件名!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }
        
        var audioPath = Path.Join(CommonConfig.PardofelisAppDataPath, "TTSAudio");

        if (!Directory.Exists(audioPath))
        {
            Directory.CreateDirectory(audioPath);
        }
        
        MessageBoxUtil.ShowToast("提示","正在生成..请不要重复点击按钮！", NotificationType.Information);
        
        VoiceOutputController.Speak(Text, true, Path.Join(audioPath, AudioName + ".wav"));
    }

    [ObservableProperty] private bool _runningState;
    [ObservableProperty] private bool _runCodeProtection;

    private PythonInstance? PythonInstance;
    private VoiceOutputController? VoiceOutputController;

    private CancellationTokenSource CurrentCancellationToken = new();

    [ObservableProperty] private string _runButtonText = "启动语音生成服务";

    [RelayCommand]
    private void Run()
    {
        if (GlobalStatus.CurrentRunningStatus == RunningStatus.Running &&
            GlobalStatus.CurrentExecutor != ExecutorName.VoiceOuput)
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("请先停止状态界面的服务!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        if (RunningState)
        {
            Thread stopThread = new Thread(async () =>
            {
                RunCodeProtection = true;

                if (CurrentCancellationToken != null && !CurrentCancellationToken.IsCancellationRequested)
                {
                    CurrentCancellationToken.Cancel();
                }

                if (PythonInstance != null)
                {
                    PythonInstance.ShutdownPythonEngine();
                    PythonInstance = null;
                }

                GlobalStatus.CurrentRunningStatus = RunningStatus.Stopped;
                GlobalStatus.CurrentExecutor = ExecutorName.None;
                
                VoiceOutputController = null;

                RunButtonText = "启动语音生成服务";
                
                MessageBoxUtil.ShowToast("提示","语音生成服务已停止！", NotificationType.Information);
                
                RunningState = false;
                
                RunCodeProtection = false;
            });
            stopThread.Start();
            return;
        }

        Thread startThread = new Thread(async () =>
        {
            RunCodeProtection = true;

            // 初始化Python环境
            Log.Information("Start python environment.");
            PythonInstance = new PythonInstance(CommonConfig.PythonRootPath);

            // 启动TTS语音输出服务
            VoiceOutputController = new(PythonInstance);
            var voiceOutputInferenceCode =
                File.ReadAllText(Path.Join(CommonConfig.PardofelisAppDataPath, @"VoiceModel\VoiceOutput\infer.py"));
            var scripts = new List<string>();
            scripts.Add(voiceOutputInferenceCode);
            var pyRes = PythonInstance.StartPythonEngine(CurrentCancellationToken, scripts);
            if (!pyRes.Status)
            {
                Log.Error("Start TTS voice output service failed.");
                MessageBoxUtil.ShowMessageBox("启动TTS语音输出服务失败！错误信息：" + pyRes.Message, "确定");
                MessageBoxUtil.ShowToast("错误","启动TTS语音输出服务失败！错误信息：" + pyRes.Message, NotificationType.Error);
            }

            GlobalStatus.CurrentRunningStatus = RunningStatus.Running;
            GlobalStatus.CurrentExecutor = ExecutorName.VoiceOuput;
            
            RunButtonText = "停止语音生成服务";
            
            MessageBoxUtil.ShowToast("提示","语音生成服务已启动！", NotificationType.Information);
            
            RunningState = true;

            RunCodeProtection = false;
        });
        startThread.Start();
    }
}