using SSMSMint.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Windows.Controls;

namespace SSMSMint.ResultsGridSearch
{
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
            DataContext = ServicesLocator.ServiceProvider.GetRequiredService<ResultsGridSearchToolWindowViewModel>();
        }

        private void FindAllSearchResultsDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!(sender is DataGrid dataGrid))
            {
                return;
            }

            if (!(dataGrid.SelectedItem is GridPosition selectedItem))
            {
                return;
            }

            if (!(DataContext is ResultsGridSearchToolWindowViewModel vm))
            {
                return;
            }

            vm.FindAllSearchResultsDoubleClick(selectedItem);
        }
    }
}