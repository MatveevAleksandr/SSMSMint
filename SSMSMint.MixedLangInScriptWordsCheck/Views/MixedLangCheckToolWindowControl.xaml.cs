using SSMSMint.MixedLangInScriptWordsCheck.Models;
using SSMSMint.MixedLangInScriptWordsCheck.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SSMSMint.MixedLangInScriptWordsCheck.Views;

/// <summary>
/// Interaction logic for MixedLangCheckToolWindowControl.xaml
/// </summary>
public partial class MixedLangCheckToolWindowControl : UserControl
{
    public MixedLangCheckToolWindowControl()
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/SSMSMint.Shared;component/Themes/MainSSMSTheme.xaml")
        };
        Application.Current.Resources.MergedDictionaries.Add(dict);

        InitializeComponent();
        DataContext = new MixedLangCheckToolWindowViewModel();
    }

    private async void MixedLangWordItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListView listView)
        {
            return;
        }

        if (listView.SelectedItem is not MixedLangWord selectedItem)
        {
            return;
        }

        if (DataContext is not MixedLangCheckToolWindowViewModel vm)
        {
            return;
        }

        await vm.MixedLangWordItemSelectionChanged(selectedItem);
    }
}
