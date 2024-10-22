using Avalonia;
using ShowMeTheXaml;
using System;
using System.IO;
using System.Text;
using Avalonia.Dialogs;
using Serilog;

namespace PardofelisUI;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().UseSkia()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "An unhandled exception occurred. Please report this error to the developers.");
        }
        finally
        {
            Log.CloseAndFlush();
            Environment.Exit(-1);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var app = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseXamlDisplay();

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            app.UseManagedSystemDialogs();
        return app;
    }
}