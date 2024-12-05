using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;

namespace PardofelisUI.Pages.About;

public partial class AboutPageViewModel() : PageBase("关于", MaterialIconKind.About, int.MinValue)
{
    [ObservableProperty]
    private DynamicUIConfig _dynamicUiConfig = new();

}