using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using PardofelisCore.Config;
using PardofelisCore.Util;

namespace PardofelisUI.Pages.VoiceInputConfig;

public class KeysToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Keys key)
        {
            return key.ToString();
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string keyString && Enum.TryParse<Keys>(keyString, out Keys key))
        {
            return key;
        }
        return Keys.Tab; // 或者返回某个默认值，比如 Keys.Tab
    }
}

public partial class VoiceInputConfigPageViewModel : PageBase
{
    private string VoiceInputConfigPath;

    [ObservableProperty] private DynamicUIConfig _dynamicUiConfig = new();

    [ObservableProperty] private bool _isPressKeyReceiveAudioInputEnabled = true;

    [ObservableProperty] private Keys _hotKey = Keys.Tab;

    public List<Keys> AvailableKeys { get; } = new()
    {
        Keys.Tab, Keys.Enter, Keys.Space, Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I,
        Keys.J,
        Keys.K, Keys.L, Keys.M, Keys.N, Keys.O, Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T,
        Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z
    };

    public VoiceInputConfigPageViewModel() : base("语音输入配置", MaterialIconKind.FileCog, int.MinValue)
    {
        VoiceInputConfigPath = Path.Join(CommonConfig.ConfigRootPath, "ApplicationConfig/VoiceInputConfig.json");
        var config = PardofelisCore.Config.VoiceInputConfig.ReadConfig(VoiceInputConfigPath);
        IsPressKeyReceiveAudioInputEnabled = config.IsPressKeyReceiveAudioInputEnabled;
        HotKey = config.HotKey;
    }

    partial void OnIsPressKeyReceiveAudioInputEnabledChanged(bool value)
    {
        SaveConfig();
    }

    partial void OnHotKeyChanged(Keys value)
    {
        SaveConfig();
    }

    private void SaveConfig()
    {
        var config = new PardofelisCore.Config.VoiceInputConfig
        {
            IsPressKeyReceiveAudioInputEnabled = IsPressKeyReceiveAudioInputEnabled,
            HotKey = HotKey
        };

        PardofelisCore.Config.VoiceInputConfig.WriteConfig(VoiceInputConfigPath, config);
    }
}