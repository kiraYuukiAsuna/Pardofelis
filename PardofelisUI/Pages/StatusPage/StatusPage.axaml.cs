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
            if (sender is TextBox textBox)
            {
                if (textBox.Text != null)
                {
                    string text = textBox.Text;
                    if (this.DataContext is StatusPageViewModel viewModel)
                    {
                        viewModel.HandleEnterKeyCommand.Execute(text);
                        e.Handled = true; // 阻止事件继续传播
                    }
                }
            }
        }
        else if (e.Key == Key.Enter)
        {
            if (sender is TextBox textBox)
            {
                textBox.Text += "\n";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            if (textBox.Text != null)
            {
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }
}