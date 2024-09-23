using System;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace PardofelisUI.Utilities;

public class MessageBoxUtil
{
    public static async Task ShowMessageBox(string message, string buttonText)
    {
        // 检查是否在UI线程上运行
        if (!Dispatcher.UIThread.CheckAccess())
        {
            // 将操作调度到UI线程
            await Dispatcher.UIThread.InvokeAsync(() => ShowMessageBox(message, buttonText));
        }
        else
        {
            // 如果已经在UI线程上，直接执行

            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent(message)
                .WithActionButton(buttonText, _ => { }, true)
                .TryShow();
        }
    }
    
    public static async Task ShowToast(string title, string content, NotificationType toastType)
    {
        // 检查是否在UI线程上运行
        if (!Dispatcher.UIThread.CheckAccess())
        {
            // 将操作调度到UI线程
            await Dispatcher.UIThread.InvokeAsync(() => ShowToast(title, content, toastType));
        }
        else
        {
            // 如果已经在UI线程上，直接执行
            DynamicUIConfig.GlobalToastManager.CreateToast()
                .WithTitle(title)
                .WithContent(content)
                .OfType(toastType)
                .Dismiss().After(TimeSpan.FromSeconds(3))
                .Dismiss().ByClicking()
                .Queue();
        }
    }
}