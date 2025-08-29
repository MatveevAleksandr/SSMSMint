using SSMSMint.MixedLangInScriptWordsCheck.Models;
using SSMSMint.MixedLangInScriptWordsCheck.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace SSMSMint.MixedLangInScriptWordsCheck.Views;

/// <summary>
/// Interaction logic for MixedLangCheckToolWindowControl.xaml
/// </summary>
public partial class MixedLangCheckToolWindowControl : UserControl
{
    public MixedLangCheckToolWindowControl()
    {
        InitializeComponent();
        DataContext = new MixedLangCheckToolWindowViewModel();
    }

    private async void MixedLangWordDoubleClickAsync(object sender, MouseButtonEventArgs e)
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

        await vm.MixedLangWordDoubleClickAsync(selectedItem);
    }
}
