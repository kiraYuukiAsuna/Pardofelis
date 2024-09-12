using System.ComponentModel;
using System.Diagnostics;
using Microsoft.SemanticKernel;

namespace FunctionCall.Agent;

public class Sing
{
    [KernelFunction]
    [Description("开始唱某一首歌")]
    public async Task<bool> SingSongAsync(Kernel kernel, [Description("歌曲名称")] string songName)
    {
        Console.WriteLine("唱歌中..." + songName);
        return true;
    }

    [KernelFunction]
    [Description("停止唱歌")]
    public async Task<bool> StopSongAsync(Kernel kernel)
    {
        Console.WriteLine("停止唱歌...");
        return true;
    }

    [KernelFunction]
    [Description("打开网页")]
    public async Task<bool> OpenWebPageAsync(Kernel kernel, [Description("网址名称")] string webPageName)
    {
        Dictionary<string, string> webPageDict = new Dictionary<string, string>();
        webPageDict.Add("百度", "https://www.baidu.com");

        string url = "";

        foreach (var name in webPageDict)
        {
            if (name.Key == webPageName)
            {
                url = name.Value;
            }
        }

        // 使用默认浏览器打开URL
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });

        Console.WriteLine("浏览器已打开。");
        return true;
    }
}