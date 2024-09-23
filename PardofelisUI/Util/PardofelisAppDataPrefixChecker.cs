using System.IO;
using PardofelisCore.Config;
using PardofelisCore.Util;
using Serilog;
using SukiUI.Dialogs;

namespace PardofelisUI.Utilities;

public class PardofelisAppDataPrefixChecker
{
    public static bool Check()
    {
        if (Directory.Exists(AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message))
        {
            Log.Information(
                $"PardofelisAppDataPrefixPath [{AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message}] exists.");
        }
        else
        {
            Log.Error(
                $"PardofelisAppDataPrefixPath [{AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message}] not exists.");
            Log.Error(
                $"Failed to find correct PardofelisAppDataPrefixPath. CurrentPath: [{AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message}]. Please set it to the correct path!");
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent($"没有找到 PardofelisAppData 所在路径，请在主页设置 PardofelisAppData 所在路径后重新启动！")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return false;
        }

        var res = AppDataDirectoryChecker.CheckAppDataDirectoryAndCreateNoExist();
        if (!res.Status)
        {
            Log.Error(res.Message);
            Log.Error(
                $"Failed to find correct PardofelisAppDataPrefixPath. CurrentPath: [{AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message}]. Please set it to the correct path!");
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent(
                    $"当前 PardofelisAppData 所在路径中没有找到 {res.Message}，请确认该路径是否正确并不缺失文件，请在主页重新设置 PardofelisAppData 所在路径后重新启动！")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return false;
        }

        return true;
    }
}