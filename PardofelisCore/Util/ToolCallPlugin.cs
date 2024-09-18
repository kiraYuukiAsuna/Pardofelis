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
    public Type Type { get; set; }
    
    public PluginAssemblyInfo(string pluginFile, Assembly assembly, Type type)
    {
        PluginFile = pluginFile;
        Assembly = assembly;
        Type = type;
    }
}

public class PluginConfigInfo
{
    public string PluginFile { get; set; }
    public Assembly Assembly { get; set; }
    public Type Type { get; set; }
    
    public PluginConfigInfo(string pluginFile, Assembly assembly, Type type)
    {
        PluginFile = pluginFile;
        Assembly = assembly;
        Type = type;
    }
}

public class FunctionCallPluginLoader
{
    public static List<PluginAssemblyInfo> PluginFiles = new();
    public static List<PluginConfigInfo> PluginConfigs = new();

    public static void Clear()
    {
        PluginFiles.Clear();
        PluginConfigs.Clear();
    }
    
    public static void EnumeratePlugin(string pluginFile)
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
                    PluginFiles.Add(new PluginAssemblyInfo(pluginFile, assembly, type));
                }
            }
        }
        catch (Exception e)
        {
            
        }
    }

    public static void SetCurrentPluginWorkingDirectory()
    {
        foreach (var plugin in PluginFiles)
        {
            bool foundConfig = false;
            foreach (var type in plugin.Assembly.GetTypes())
            {
                if (type.Name == "Config")
                {
                    PluginConfigs.Add(new PluginConfigInfo(plugin.PluginFile, plugin.Assembly, type));
                    
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

    public static void AddPlugin(IKernelBuilder builder)
    {
        foreach (var plugin in PluginFiles)
        {
            if(plugin.Type.FullName == null)
            {
                continue;
            }
            
            var instance = plugin.Type.Assembly.CreateInstance(plugin.Type.FullName);

            if(instance == null)
            {
                continue;
            }
            
            builder.Plugins.AddFromObject(instance);
        }
    }
}
