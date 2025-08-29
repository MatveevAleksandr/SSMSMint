using SSMSMint.ResultsGridSearch.Models;
using SSMSMint.ResultsGridSearch.ViewModels;
using NLog;
using System.Windows.Controls;
using SSMSMint.ResultsGridSearch.Models;

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
        InitializeComponent();
        _logger.Info($"{nameof(ResultsGridSearchToolWindowControl)} initialized");
        DataContext = new ResultsGridSearchToolWindowViewModel();
    }

    private void FindAllSearchResultsDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        vm.FindAllSearchResultsDoubleClick(selectedItem);
    }
}