using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Templates;
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
            else if (property.PropertyType == typeof(bool))
            {
                var checkBox = new CheckBox
                {
                    Content = property.Name,  // 使用属性名作为 CheckBox 的标签
                    IsChecked = (bool)property.GetValue(model)  // 设置初始状态
                };

                // 双向绑定
                checkBox.Bind(CheckBox.IsCheckedProperty, new Binding(property.Name)
                {
                    Mode = BindingMode.TwoWay
                });

                control = checkBox;
            }
            else if (property.PropertyType == typeof(List<string>))
            {
                var listBox = new ListBox();
                listBox.Bind(ItemsControl.ItemsSourceProperty, new Binding(property.Name));
                control = listBox;
            }
            else if (property.PropertyType == typeof(List<KeyValuePair<bool, string>>))
            {
                var itemsControl = new ItemsControl();
                itemsControl.ItemTemplate = new FuncDataTemplate<KeyValuePair<bool, string>>((item, _) =>
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal };
        
                    var checkBox = new CheckBox { Margin = new Thickness(0, 0, 5, 0) };
                    checkBox.IsChecked = item.Key;  // 直接设置初始状态
                    checkBox.GetObservable(CheckBox.IsCheckedProperty).Subscribe(isChecked =>
                    {
                        var list = (List<KeyValuePair<bool, string>>)property.GetValue(model);
                        var index = list.IndexOf(item);
                        if (index != -1)
                        {
                            // 使用 isChecked ?? false 来处理所有可能的状态
                            list[index] = new KeyValuePair<bool, string>(isChecked ?? false, item.Value);
                            property.SetValue(model, list);
                        }
                    });
        
                    var textBlock = new TextBlock();
                    textBlock.Text = item.Value;  // 直接设置文本
        
                    panel.Children.Add(checkBox);
                    panel.Children.Add(textBlock);
        
                    return panel;
                });

                itemsControl.Bind(ItemsControl.ItemsSourceProperty, new Binding(property.Name));
                control = itemsControl;
            }else if (property.PropertyType == typeof(List<KeyValuePair<string, string>>))
            {
                var itemsControl = new ItemsControl();
                itemsControl.ItemTemplate = new FuncDataTemplate<KeyValuePair<string, string>>((item, _) =>
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal };
        
                    var label = new TextBlock
                    {
                        Text = item.Key,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                        Width = 100  // 设置一个固定宽度，您可以根据需要调整
                    };
        
                    var textBox = new TextBox
                    {
                        Text = item.Value,
                        Width = 200  // 设置一个固定宽度，您可以根据需要调整
                    };

                    // 处理文本变化
                    textBox.GetObservable(TextBox.TextProperty).Subscribe(newText =>
                    {
                        var list = (List<KeyValuePair<string, string>>)property.GetValue(model);
                        var index = list.IndexOf(item);
                        if (index != -1)
                        {
                            list[index] = new KeyValuePair<string, string>(item.Key, newText);
                            property.SetValue(model, list);
                        }
                    });
        
                    panel.Children.Add(label);
                    panel.Children.Add(textBox);
        
                    return panel;
                });

                itemsControl.Bind(ItemsControl.ItemsSourceProperty, new Binding(property.Name));
                control = itemsControl;
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