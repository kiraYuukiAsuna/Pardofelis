using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PardofelisCore.Config;
using PardofelisCore.Util;
using SukiUI.Dialogs;

namespace PardofelisUI.Pages.HomePage;

public partial class HomePageViewModel : PageBase
{
    [ObservableProperty] private string _pardofelisAppDataPrefixPath;

    public HomePageViewModel(): base("主页", MaterialIconKind.Home, int.MinValue)
    {
        PardofelisAppDataPrefixPath = AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message;
    }

    [RelayCommand]
    private void UpdatePath()
    {
        var result = AppDataDirectoryChecker.SetCurrentPardofelisAppDataPrefixPath(PardofelisAppDataPrefixPath.Trim(' '));

        if (result.Status)
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("设置成功！")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
        else
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("设置失败！"+result.Message)
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            PardofelisAppDataPrefixPath = AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message;
        }
    }
}