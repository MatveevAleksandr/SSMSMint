using SSMSMint.Core.UI.Interfaces;
using SSMSMint.Core.UI.Models;
using SSMSMint.Core.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SSMSMint.Core.UI.View;

/// <summary>
/// Interaction logic for MixedLangCheckControl.xaml
/// </summary>
public partial class MixedLangCheckControl : UserControl
{
    public MixedLangCheckControl()
    {
        InitializeComponent();
    }

    public void Initialize(IToolWindowParams twParams)
    {
        if (twParams is not MixedLangToolWindowParams mixedLangParams)
            throw new Exception($"{twParams.GetType()} is incorrect type for ${nameof(MixedLangCheckControl)}");

        var dict = new ResourceDictionary
        {
            Source = new Uri(mixedLangParams.ThemeUriStr)
        };
        Application.Current.Resources.MergedDictionaries.Add(dict);

        DataContext = new MixedLangCheckViewModel(mixedLangParams.MixedLangWords, mixedLangParams.TdManager);
    }

    private void MixedLangWordItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListView listView)
        {
            return;
        }

        if (listView.SelectedItem is not MixedLangWord selectedItem)
        {
            return;
        }

        if (DataContext is not MixedLangCheckViewModel vm)
        {
            return;
        }

        vm.MixedLangWordItemSelectionChanged(selectedItem);
    }
}
