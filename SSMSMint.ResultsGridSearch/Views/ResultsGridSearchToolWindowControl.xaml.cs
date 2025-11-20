using NLog;
using SSMSMint.ResultsGridSearch.Models;
using SSMSMint.ResultsGridSearch.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SSMSMint.ResultsGridSearch.Views;

/// <summary>
/// Interaction logic for ResultsGridSearchToolWindowControl.
/// </summary>
public partial class ResultsGridSearchToolWindowControl : UserControl
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsGridSearchToolWindowControl"/> class.
    /// </summary>
    public ResultsGridSearchToolWindowControl()
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/SSMSMint.Shared;component/Themes/MainSSMSTheme.xaml")
        };
        Application.Current.Resources.MergedDictionaries.Add(dict);

        InitializeComponent();
        _logger.Info($"{nameof(ResultsGridSearchToolWindowControl)} initialized");
        DataContext = new ResultsGridSearchToolWindowViewModel();
    }

    private void SearchResultItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListView listView)
        {
            return;
        }

        if (listView.SelectedItem is not GridCell selectedItem)
        {
            return;
        }

        if (DataContext is not ResultsGridSearchToolWindowViewModel vm)
        {
            return;
        }

        vm.SearchResultItemSelectionChanged(selectedItem);
    }
}