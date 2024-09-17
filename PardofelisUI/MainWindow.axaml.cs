using Avalonia.Controls;
using Avalonia.Interactivity;
using PardofelisCore.Config;
using PardofelisCore.Util;
using PardofelisUI.Pages.StatusPage;
using Serilog;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Models;
using System.IO;
using System;

namespace PardofelisUI;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();

        this.Closing += OnMainWindowClosing;

        DialogHost.Manager = DynamicUIConfig.GlobalDialogManager;

        if (Directory.Exists(CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath))
        {
            Log.Information($"PardofelisAppDataPrefixPath [{CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath}] exists.");
        }
        else
        {
            Log.Error($"PardofelisAppDataPrefixPath [{CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath}] not exists.");
            Log.Error($"Failed to find correct PardofelisAppDataPrefixPath. CurrentPath: [{CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath}]. Please set it to the correct path!");
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent($"没有找到 PardofelisAppData 所在路径，请在主页设置 PardofelisAppData 所在路径后重新启动！")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
        var res = AppDataDirectoryChecker.CheckAppDataDirectoryAndCreateNoExist();
        if (!res.Status)
        {
            Log.Error(res.Message);
            Log.Error($"Failed to find correct PardofelisAppDataPrefixPath. CurrentPath: [{CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath}]. Please set it to the correct path!");
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent($"当前 PardofelisAppData 所在路径中没有找到 {res.Message}，请确认该路径是否正确并不缺失文件，请在主页重新设置 PardofelisAppData 所在路径后重新启动！")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
    }

    private void MenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (e.Source is not MenuItem mItem) return;
        if (mItem.DataContext is not SukiColorTheme cTheme) return;
        vm.ChangeTheme(cTheme);
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (var page in (DataContext as MainWindowViewModel).Pages)
        {
            switch (page)
            {
                case StatusPageViewModel statusPageViewModel:
                {
                    statusPageViewModel.StopIfRunning();
                    break;
                }
            }
        }
    }
}