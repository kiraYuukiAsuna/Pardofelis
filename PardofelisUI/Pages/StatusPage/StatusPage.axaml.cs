using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace PardofelisUI.Pages.StatusPage;

public partial class StatusPage : UserControl
{
    public StatusPage()
    {
        InitializeComponent();
    }

    private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                string text = textBox.Text;
                var viewModel = this.DataContext as StatusPageViewModel;
                if (viewModel != null)
                {
                    viewModel.HandleEnterKeyCommand.Execute(text);
                    e.Handled = true; // 阻止事件继续传播
                }
            }
        }else if(e.Key == Key.Enter)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.Text += "\n";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox != null)
        {
            textBox.CaretIndex = textBox.Text.Length;
        }
    }
}