using SSMSMint.Core.UI.Interfaces;
using SSMSMint.Core.UI.Models;
using SSMSMint.ResultsGridSearch.ViewModels;
using NLog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SSMSMint.Core.UI.View;

/// <summary>
/// Interaction logic for ResultsGridSearchControl.
/// </summary>
public partial class ResultsGridSearchControl : UserControl
{
    private readonly Logger logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsGridSearchControl"/> class.
    /// </summary>
    public ResultsGridSearchControl()
    {
        InitializeComponent();
        logger.Info($"{nameof(ResultsGridSearchControl)} initialized");
    }

    public void Initialize(IToolWindowParams twParams)
    {
        if (twParams is not ResultsGridsSearchToolWindowParams rgsParams)
            throw new Exception($"{twParams.GetType()} is incorrect type for ${nameof(ResultsGridSearchControl)}");

        var dict = new ResourceDictionary
        {
            Source = new Uri(rgsParams.ThemeUriStr)
        };
        Application.Current.Resources.MergedDictionaries.Add(dict);

        DataContext = new ResultsGridSearchToolWindowViewModel(rgsParams.UINotificationManager, rgsParams.Feature);
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