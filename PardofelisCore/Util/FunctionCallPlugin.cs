using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace PardofelisCore.Util;

public class PluginAssemblyInfo
{
    public string PluginFile { get; set; }
    public Assembly Assembly { get; set; }
    public PluginAssemblyInfo(string pluginFile, Assembly assembly)
    {
        PluginFile = pluginFile;
        Assembly = assembly;
    }
}

public class FunctionCallPluginLoader
{
    public static List<PluginAssemblyInfo> PluginFiles = new();

    public static void LoadPlugin(string pluginFile, IKernelBuilder builder)
    {
        Assembly assembly = Assembly.LoadFile(pluginFile);
        try
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {

                var methods = type.GetMethods();
                bool foundKernelFunction = false;
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(KernelFunctionAttribute), false);
                    if (attributes.Length > 0)
                    {
                        foundKernelFunction = true;
                        Log.Information($"Found KernelFunction: {method.Name} in {type.FullName}");
                    }
                }
                if (!foundKernelFunction)
                {
                    continue;
                }
                else
                {
                    var instance = type.Assembly.CreateInstance(type.FullName);

                    builder.Plugins.AddFromObject(instance);

                    PluginFiles.Add(new PluginAssemblyInfo(pluginFile, assembly));
                }
            }
        }
        catch (Exception e)
        {
            
        }
    }

    public static void SetConfig()
    {
        foreach (var plugin in PluginFiles)
        {
            bool foundConfig = false;
            foreach (var type in plugin.Assembly.GetTypes())
            {
                if (type.Name == "Config")
                {
                    foundConfig = true;
                    FieldInfo fieldInfo = type.GetField("CurrentPluginWorkingDirectory", BindingFlags.Static | BindingFlags.Public);
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(null, Path.GetDirectoryName(plugin.PluginFile));

                        string currentValue = (string)fieldInfo.GetValue(null);
                        Log.Information("CurrentPluginWorkingDirectory Field found and set to: " + currentValue);
                    }
                    else
                    {
                        Log.Information("CurrentPluginWorkingDirectory Field not found.");
                    }
                }
            }

            if (!foundConfig)
            {
                Log.Information("Type 'Config' not found in the assembly.");
            }
        }
    }
}
