using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PardofelisCore.Config;
using PardofelisCore.Util;

namespace PardofelisUI.Pages.HomePage;

public partial class HomePageViewModel : PageBase
{
    [ObservableProperty] private string _pardofelisAppDataPrefixPath;

    public HomePageViewModel(): base("主页", MaterialIconKind.Home, int.MinValue)
    {
        PardofelisAppDataPrefixPath = CommonConfig.PardofelisAppSettings.PardofelisAppDataPrefixPath;
    }

    [RelayCommand]
    private void UpdatePath()
    {
        AppDataDirectoryChecker.SetCurrentPardofelisAppDataPrefixPath(PardofelisAppDataPrefixPath.Trim(' '));
    }
}