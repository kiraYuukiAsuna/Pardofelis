using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using Newtonsoft.Json;
using PardofelisCore.Config;
using PardofelisCore.Util;
using Serilog;
using SukiUI.Dialogs;

namespace PardofelisUI.Pages.ExtraConfig;

public class TabViewModel : ObservableObject
{
    public string Header { get; set; }
    public object Content { get; set; }

    public TabViewModel(string header, object content)
    {
        Header = header;
        Content = content;
    }
}

public partial class ExtraConfigPageViewModel : PageBase
{
    [ObservableProperty] private AvaloniaList<TabViewModel> _tabs = [];

    public ExtraConfigPageViewModel() : base("额外配置", MaterialIconKind.FileCog, int.MinValue)
    {
        FunctionCallPluginLoader.Clear();
        foreach (var pluginFolder in Directory.GetDirectories(CommonConfig.ToolCallPluginRootPath))
        {
            var pluginFiles = Directory.GetFiles(pluginFolder);

            foreach (var file in pluginFiles)
            {
                if (Path.GetExtension(file) == ".dll")
                {
                    FunctionCallPluginLoader.EnumeratePlugin(file);
                }
            }
        }
        FunctionCallPluginLoader.SetCurrentPluginWorkingDirectory();

        foreach (var plugin in FunctionCallPluginLoader.PluginConfigs)
        {
            StackPanel stackPanel;
            try
            {
                var instance = plugin.Type.Assembly.CreateInstance(plugin.Type.FullName);
                if (instance == null)
                {
                    continue;
                }

                try
                {
                    instance.GetType().GetMethod("Init")?.Invoke(instance, new object[] { });
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Failed to init plugin {PluginFile}", plugin.PluginFile);
                    DynamicUIConfig.GlobalDialogManager.CreateDialog()
                        .WithTitle("错误！")
                        .WithContent("初始化插件配置信息失败！" + exception.Message)
                        .WithActionButton("确定", _ => { }, true)
                        .TryShow();
                }
                
                try
                {
                    var obj = (instance.GetType().GetMethod("ReadConfig")?.Invoke(instance, new object[] { }));
                    if (obj != null)
                    {
                        instance = obj;
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Failed to read config for {PluginFile}", instance.GetType().Name);
                    DynamicUIConfig.GlobalDialogManager.CreateDialog()
                        .WithTitle("错误！")
                        .WithContent("读取插件配置信息失败！" + exception.Message)
                        .WithActionButton("确定", _ => { }, true)
                        .TryShow();
                }

                stackPanel = GenerateControls(instance, plugin);
                Tabs.Add(new TabViewModel(Path.GetFileName(plugin.PluginFile), stackPanel));
            }
            catch (Exception e)
            {
                DynamicUIConfig.GlobalDialogManager.CreateDialog()
                    .WithTitle("错误！")
                    .WithContent("生成插件配置页面失败！" + e.Message)
                    .WithActionButton("确定", _ => { }, true)
                    .TryShow();
                Log.Error(e, "Failed to generate controls for plugin {PluginFile}", plugin.PluginFile);
            }
        }
    }

    public static StackPanel GenerateControls(object model, PluginConfigInfo plugin)
    {
        var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(10), Spacing = 5 };
        var properties = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);


        foreach (var property in properties)
        {
            // Filter properties: Only readable properties, skip indexers
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            var value = property.GetValue(model);
            var label = new TextBlock { Text = property.Name + ": " };

            Control control = null;

            if (property.PropertyType == typeof(string))
            {
                control = new TextBox();
                control.Bind(TextBox.TextProperty, new Binding(property.Name, BindingMode.TwoWay));
            }
            else if (property.PropertyType == typeof(int))
            {
                control = new TextBox();
                control.Bind(TextBox.TextProperty, new Binding(property.Name, BindingMode.TwoWay));
            }
            else if (property.PropertyType == typeof(float))
            {
                control = new TextBox();
                control.Bind(TextBox.TextProperty, new Binding(property.Name, BindingMode.TwoWay));
            }
            else if (property.PropertyType == typeof(List<string>))
            {
                var listBox = new ListBox();
                listBox.Bind(ItemsControl.ItemsSourceProperty, new Binding(property.Name));
                control = listBox;
            }
            else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
            {
                continue;
            }

            if (control != null)
            {
                control.DataContext = model;
                stackPanel.Children.Add(label);
                stackPanel.Children.Add(control);
            }
        }
        
        var writeConfigButton = new Button { Content = "保存配置" };
        writeConfigButton.Click += (sender, e) =>
        {
            try
            {
                model.GetType().GetMethod("WriteConfig")?.Invoke(model, new object[] { model });
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to write config for {PluginFile}", model.GetType().Name);
                DynamicUIConfig.GlobalDialogManager.CreateDialog()
                    .WithTitle("错误！")
                    .WithContent("保存配置信息失败！" + exception.Message)
                    .WithActionButton("确定", _ => { }, true)
                    .TryShow();
            }
        };
        stackPanel.Children.Add(writeConfigButton);
        return stackPanel;
    }
}