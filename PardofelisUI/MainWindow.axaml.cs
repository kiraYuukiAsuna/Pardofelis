using Avalonia.Controls;
using Avalonia.Interactivity;
using PardofelisCore.Config;
using PardofelisCore.Logger;
using PardofelisCore.Util;
using PardofelisUI.Pages.StatusPage;
using Serilog;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Models;
using PardofelisUI.Utilities;

namespace PardofelisUI;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();

        this.Closing += OnMainWindowClosing;

        DialogHost.Manager = DynamicUIConfig.GlobalDialogManager;
        ToastHost.Manager = DynamicUIConfig.GlobalToastManager;

        DataContext = new MainWindowViewModel();
            
        var model = DataContext as MainWindowViewModel;
        if (model == null)
        {
            Log.Error("MainWindow DataContext is not MainWindowViewModel.");
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent($"MainWindow DataContext as MainWindowViewModel failed!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        if (PardofelisAppDataPrefixChecker.Check())
        {
            GlobalLogger.Initialize(CommonConfig.LogRootPath);
            GlobalConfig.Instance = GlobalConfig.ReadConfig();
        }
        else
        {
            GlobalLogger.Initialize(CommonConfig.CurrentWorkingDirectory);
            GlobalConfig.Instance = GlobalConfig.ReadConfig();
        }
        
        model.LoadPages();

        KeyboardHookEntry.RunHookInOtherThread();
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
        var avaloniaList = (DataContext as MainWindowViewModel)?.Pages;
        if (avaloniaList != null)
            foreach (var page in avaloniaList)
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