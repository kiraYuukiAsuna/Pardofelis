using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace PardofelisUI.Pages.SettingsPage;

public partial class SettingsPageViewModel() : PageBase("设置", MaterialIconKind.FileCog, int.MinValue)
{
    [ObservableProperty]
    private DynamicUIConfig _dynamicUiConfig = new ();

}