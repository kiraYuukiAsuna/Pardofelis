using SingPlugin;


var plugin = new SingPlugin.SingPlugin();

await plugin.StartSingAsync(null, "天下");


while (true)
{
    await Task.Delay(5000);
    plugin.StopSingAsync(null);
    await Task.Delay(5000);
    await plugin.StartSingAsync(null, "赤伶");
    await Task.Delay(5000);
    await plugin.StartSingAsync(null, "天下");
}