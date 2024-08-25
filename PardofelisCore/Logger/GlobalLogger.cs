using PardofelisCore.Config;
using Serilog;

namespace PardofelisCore.Logger;

public class GlobalLogger
{
    public static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Join(CommonConfig.LogRootPath, "PardofelisCore.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}