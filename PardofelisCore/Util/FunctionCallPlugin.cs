using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace PardofelisCore.Util;

public class FunctionCallPluginLoader
{
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
                }
            }
        }
        catch (Exception e)
        {
            
        }
    }
}
