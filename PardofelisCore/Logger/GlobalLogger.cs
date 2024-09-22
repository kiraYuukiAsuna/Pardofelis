using PardofelisCore.Config;
using Serilog;

namespace PardofelisCore.Logger;

public class GlobalLogger
{
    public static void Initialize(string mainLogRootPath)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Join(mainLogRootPath, "PardofelisCore.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}