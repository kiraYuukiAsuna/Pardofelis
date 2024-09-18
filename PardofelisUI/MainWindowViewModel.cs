using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PardofelisCore.Config;
using PardofelisCore.Util;
using PardofelisUI.CustomTheme;
using PardofelisUI.Pages;
using PardofelisUI.Pages.About;
using PardofelisUI.Pages.ExtraConfig;
using PardofelisUI.Pages.HomePage;
using PardofelisUI.Pages.LlmConfig;
using PardofelisUI.Pages.StatusPage;
using PardofelisUI.Pages.VoiceInputConfig;
using PardofelisUI.Utilities;
using Serilog;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Models;
using TextMateSharp.Themes;
using BertVits2ConfigPageViewModel = PardofelisUI.Pages.VoiceOutputConfig.VoiceOutputConfigPageViewModel;
using CharacterPresetConfigPageViewModel = PardofelisUI.Pages.CharacterPresetPage.CharacterPresetConfigPageViewModel;

namespace PardofelisUI;

public partial class MainWindowViewModel : PageBase
{
    [ObservableProperty]
    private DynamicUIConfig _dynamicUIConfig;

    public void LoadPages()
    {
        Pages = new AvaloniaList<PageBase>
        {
            new HomePageViewModel(),
            new StatusPageViewModel(),
            new CharacterPresetConfigPageViewModel(),
            new LlmConfigPageViewModel(),
            new BertVits2ConfigPageViewModel(),
            new VoiceInputConfigPageViewModel(),
            new AboutPageViewModel(),
            new ExtraConfigPageViewModel()
        };
        _activePage = Pages[0];

        _pageNavigationService.NavigationRequested += pageType =>
        {
            var page = Pages.FirstOrDefault(x => x.GetType() == pageType);
            if (page is null || ActivePage?.GetType() == pageType) return;
            ActivePage = page;
        };
    }
    
    public MainWindowViewModel() : base("满穗AI助手", MaterialIconKind.Home)
    {        
        _theme = SukiTheme.GetInstance();
        
        ColorThemes = _theme.ColorThemes;
        BaseTheme = _theme.ActiveBaseTheme;

        // Subscribe to the base theme changed events
        _theme.OnBaseThemeChanged += variant =>
        {
            BaseTheme = variant;
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent($"Successfully Changed Theme: Changed Theme To {variant}")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        };

        // Subscribe to the color theme changed events
        _theme.OnColorThemeChanged += theme =>
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent($"Successfully Changed Color: Changed Color To {theme.DisplayName}.")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        };

    }

    public static void OpenUrlInternal(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo(url.Replace("&", "^&")) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
    }

    [RelayCommand]
    private static void OpenUrl(string url) => OpenUrlInternal(url);

    [ObservableProperty] 
    private IAvaloniaList<PageBase> _pages=new AvaloniaList<PageBase>();
    [ObservableProperty] private PageBase? _activePage;

    private PageNavigationService _pageNavigationService = new();

    [ObservableProperty] private IAvaloniaReadOnlyList<SukiColorTheme> _colorThemes=new AvaloniaList<SukiColorTheme>();

    [ObservableProperty] private ThemeVariant _baseTheme;
    [ObservableProperty] private bool _animationsEnabled;
    [ObservableProperty] private bool _titleBarVisible = true;

    private readonly SukiTheme _theme;

    [RelayCommand]
    private Task ToggleAnimations()
    {
        AnimationsEnabled = !AnimationsEnabled;
        var title = AnimationsEnabled ? "Animation Enabled" : "Animation Disabled";
        var content = AnimationsEnabled
            ? "Background animations are now enabled."
            : "Background animations are now disabled.";

        DynamicUIConfig.GlobalDialogManager.CreateDialog()
            .WithTitle(title)
            .WithContent(content)
            .WithActionButton("确定", _ => { }, true)
            .TryShow();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ToggleBaseTheme() =>
        _theme.SwitchBaseTheme();

    public void ChangeTheme(SukiColorTheme theme) =>
        _theme.ChangeColorTheme(theme);

    [RelayCommand]
    private void CreateCustomTheme()
    {
        DynamicUIConfig.GlobalDialogManager.CreateDialog()
            .WithViewModel(dialog => new CustomThemeDialogViewModel(_theme)).Dismiss().ByClickingBackground().TryShow();
    }
}