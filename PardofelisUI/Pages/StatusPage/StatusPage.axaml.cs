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

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (e.KeyModifiers == KeyModifiers.Control))
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                string text = textBox.Text;
                var viewModel = this.DataContext as StatusPageViewModel;
                if (viewModel != null)
                {
                    viewModel.HandleEnterKeyCommand.Execute(text);
                }
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