using System;
using System.IO;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PardofelisCore.Config;
using Serilog;
using SukiUI.Dialogs;


namespace PardofelisUI.Pages.LlmConfig;

public partial class LlmConfigPageViewModel : PageBase
{
    public LlmConfigPageViewModel() : base("大语言模型配置", MaterialIconKind.FileCog, int.MinValue)
    {
        RescanConfig();
    }

    private string m_ModelConfigRootPath = Path.Join(CommonConfig.ConfigRootPath, "ModelConfig");

    [ObservableProperty] private string _configName = "";

    // LocalLlmCreateInfo
    [ObservableProperty] private UInt32 _localContextSize;
    [ObservableProperty] private float _temperature;
    [ObservableProperty] private Int32 _nGpuLayers;
    [ObservableProperty] private UInt32 _seed;
    [ObservableProperty] private bool _useMemoryLock;
    [ObservableProperty] private UInt32 _batchThreads;
    [ObservableProperty] private UInt32 _threads;
    [ObservableProperty] private UInt32 _batchSize;
    [ObservableProperty] private bool _flashAttention;

    // OnlineLlmCreateInfo
    [ObservableProperty] private float _onlineTemperature;
    [ObservableProperty] private int _onlineContextSize;
    [ObservableProperty] private string _onlineModelName = "";
    [ObservableProperty] private string _onlineModelUrl = "";
    [ObservableProperty] private string _onlineModelApiKey = "";

    [ObservableProperty] public AvaloniaList<string> _modelParameterConfigList = [];
    [ObservableProperty] private string _selectedModelParameterConfig = "";
    [ObservableProperty] private string _newConfigFileName = "";

    private void ReloadConfig(string configPath)
    {
        m_ModelConfigRootPath = Path.Join(CommonConfig.ConfigRootPath, "ModelConfig");
        
        if (configPath.Length == 0)
        {
            return;
        }

        if (!File.Exists(configPath))
        {
            return;
        }

        var config = ModelParameterConfig.ReadConfig(configPath);
        ConfigName = config.Name;
        LocalContextSize = config.LocalLlmCreateInfo.ContextSize;
        Temperature = config.LocalLlmCreateInfo.Temperature;
        NGpuLayers = config.LocalLlmCreateInfo.NGpuLayers;
        Seed = config.LocalLlmCreateInfo.Seed;
        UseMemoryLock = config.LocalLlmCreateInfo.UseMemoryLock;
        BatchThreads = config.LocalLlmCreateInfo.BatchThreads;
        Threads = config.LocalLlmCreateInfo.Threads;
        BatchSize = config.LocalLlmCreateInfo.BatchSize;
        FlashAttention = config.LocalLlmCreateInfo.FlashAttention;

        OnlineTemperature = config.OnlineLlmCreateInfo.Temperature;
        OnlineContextSize = config.OnlineLlmCreateInfo.ContextSize;
        OnlineModelName = config.OnlineLlmCreateInfo.OnlineModelName;
        OnlineModelUrl = config.OnlineLlmCreateInfo.OnlineModelUrl;
        OnlineModelApiKey = config.OnlineLlmCreateInfo.OnlineModelApiKey;

        SelectedModelParameterConfig = Path.GetFileName(configPath);
    }

    [RelayCommand]
    private void RescanConfig()
    {
        m_ModelConfigRootPath = Path.Join(CommonConfig.ConfigRootPath, "ModelConfig");

        SelectedModelParameterConfig = "";
        ModelParameterConfigList.Clear();

        var configFileNames = ModelParameterConfig.GetAllConfigFileNames(m_ModelConfigRootPath);
        foreach (var fileName in configFileNames)
        {
            ModelParameterConfigList.Add(fileName);
        }

        if (ModelParameterConfigList.Count > 0)
        {
            SelectedModelParameterConfig = ModelParameterConfigList[0];
        }
    }

    partial void OnSelectedModelParameterConfigChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        ReloadConfig(m_ModelConfigRootPath + "/" + value);
    }


    [RelayCommand]
    private void CreateNewConfig()
    {
        if (NewConfigFileName.Length == 0)
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("请输入新配置文件名称!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        var configPath = m_ModelConfigRootPath + "/" + NewConfigFileName + ".json";
        if (File.Exists(configPath))
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("同名文件已存在!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        var config = new ModelParameterConfig();
        ModelParameterConfig.WriteConfig(configPath, config);
        DynamicUIConfig.GlobalDialogManager.CreateDialog()
            .WithTitle("提示！")
            .WithContent("创建新配置文件成功!")
            .WithActionButton("确定", _ => { }, true)
            .TryShow();
        RescanConfig();
        ReloadConfig(configPath);
    }


    [RelayCommand]
    private void DeleteConfig()
    {
        if (SelectedModelParameterConfig == null || SelectedModelParameterConfig.Length == 0)
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("请选择配置文件!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        var configPath = m_ModelConfigRootPath + "/" + SelectedModelParameterConfig;
        if (!File.Exists(configPath))
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("删除成功!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        try
        {
            File.Delete(configPath);
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("删除成功!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
        catch (Exception e)
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("删除失败! 错误信息：" + e.Message)
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }

        RescanConfig();
    }

    public static FilePickerFileType ConfigFilePickerFileType { get; set; } = new("LlmConfig")
    {
        Patterns = new[] { "*.json" },
    };

    [RelayCommand]
    private async void ImportConfig()
    {
        try
        {
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel =
                TopLevel.GetTopLevel(((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime)
                    .MainWindow);

            // Start async operation to open the dialog.
            var files = await topLevel?.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "选择配置文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { ConfigFilePickerFileType }
            });

            if (files.Count == 0)
            {
                return;
            }

            if (File.Exists(m_ModelConfigRootPath + "/" + files[0].Name))
            {
                DynamicUIConfig.GlobalDialogManager.CreateDialog()
                    .WithTitle("提示！")
                    .WithContent("同名配置文件已经存在，导入失败!")
                    .WithActionButton("确定", _ => { }, true)
                    .TryShow();
                return;
            }

            File.Copy(files[0].Path.LocalPath, m_ModelConfigRootPath + "/" + files[0].Name, true);
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("导入配置文件： " + files[0].Path.LocalPath + " 成功!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            RescanConfig();
        }
        catch (Exception e)
        {
            Log.Error("导入配置文件失败! 错误信息：" + e.Message);
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("导入配置文件失败! 错误信息：" + e.Message)
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
    }

    [RelayCommand]
    private async void ExportConfig()
    {
        if (SelectedModelParameterConfig.Length == 0)
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("请选择要导出的配置文件!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        var configPath = m_ModelConfigRootPath + "/" + SelectedModelParameterConfig;
        if (!File.Exists(configPath))
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("选中的配置文件不存在!导出失败！")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel =
            TopLevel.GetTopLevel(((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime)
                .MainWindow);

        // Start async operation to open the dialog.
        var folders = await topLevel?.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false
        });

        if (folders.Count == 0)
        {
            return;
        }

        File.Copy(m_ModelConfigRootPath + "/" + SelectedModelParameterConfig,
            folders[0].Path.LocalPath + "/" + Path.GetFileName(SelectedModelParameterConfig), true);
        DynamicUIConfig.GlobalDialogManager.CreateDialog()
            .WithTitle("提示！")
            .WithContent("导入配置文件： " + SelectedModelParameterConfig + " 到目录 " + folders[0].Path.LocalPath + " 成功!")
            .WithActionButton("确定", _ => { }, true)
            .TryShow();
    }

    [RelayCommand]
    private void SaveConfig()
    {
        if (string.IsNullOrEmpty(SelectedModelParameterConfig))
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("请选择配置文件!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        var configPath = m_ModelConfigRootPath + "/" + SelectedModelParameterConfig;
        if (!File.Exists(configPath))
        {
            return;
        }

        var config = new ModelParameterConfig()
        {
            Name = ConfigName,
            LocalLlmCreateInfo = new LocalLlmCreateInfo(LocalContextSize, Temperature, NGpuLayers, Seed,
                UseMemoryLock, BatchThreads, Threads, BatchSize, FlashAttention),
            OnlineLlmCreateInfo = new OnlineLlmCreateInfo(OnlineTemperature, OnlineContextSize, OnlineModelName,
                OnlineModelUrl,
                OnlineModelApiKey)
        };
        ModelParameterConfig.WriteConfig(configPath, config);
        DynamicUIConfig.GlobalDialogManager.CreateDialog()
            .WithTitle("提示！")
            .WithContent("保存配置文件成功!")
            .WithActionButton("确定", _ => { }, true)
            .TryShow();
    }
}