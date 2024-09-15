using Avalonia;
using ShowMeTheXaml;
using System;
using Avalonia.Dialogs;
using Serilog;
using System.IO;
using PardofelisCore.Config;

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
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "An unhandled exception occurred." +
                         "Please report this error to the developers.");
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
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Join(CommonConfig.LogRootPath, "Application.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var app = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseXamlDisplay();

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            app.UseManagedSystemDialogs();
        return app;
    }
}