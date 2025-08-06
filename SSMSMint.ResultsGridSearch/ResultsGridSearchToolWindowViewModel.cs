using CommunityToolkit.Mvvm.Input;
using NLog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SSMSMint.ResultsGridSearch
{
    internal class ResultsGridSearchToolWindowViewModel : INotifyPropertyChanged
    {
        private readonly SearchProcessor _searchService;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FindAllCommand.NotifyCanExecuteChanged();
                FindNextCommand.NotifyCanExecuteChanged();
                FindPrevCommand.NotifyCanExecuteChanged();
            }
        }

        private ObservableCollection<GridPosition> _findAllSearchResults = new ObservableCollection<GridPosition>();
        public ObservableCollection<GridPosition> FindAllSearchResults
        {
            get => _findAllSearchResults;
            set
            {
                _findAllSearchResults = value;
                OnPropertyChanged(nameof(FindAllSearchResults));
                OnPropertyChanged(nameof(HasFindAllSearchResults));
                OnPropertyChanged(nameof(FindAllSearchResultsCountText));
            }
        }

        public string FindAllSearchResultsCountText => $"Found: {FindAllSearchResults.Count}";
        public bool HasFindAllSearchResults => FindAllSearchResults.Count > 0;

        public bool MatchCase { get; set; }
        public GridLookIn GridLookIn { get; set; }

        public RelayCommand FindAllCommand { get; }
        public RelayCommand FindNextCommand { get; }
        public RelayCommand FindPrevCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ResultsGridSearchToolWindowViewModel(SearchProcessor searchService)
        {
            _searchService = searchService;

            FindAllCommand = new RelayCommand(
                execute: FindAll,
                canExecute: () => !string.IsNullOrEmpty(SearchText)
                );
            FindNextCommand = new RelayCommand(
                execute: FindNext,
                canExecute: () => !string.IsNullOrEmpty(SearchText)
                );
            FindPrevCommand = new RelayCommand(
                execute: FindPrev,
                canExecute: () => !string.IsNullOrEmpty(SearchText)
                );
        }

        public void FindAllSearchResultsDoubleClick(GridPosition selectedItem)
        {
            _searchService.FocusCell(selectedItem);
        }

        private void FindNext()
        {
            _logger.Info($"{nameof(FindNext)} called with Text = '{SearchText}'; Match Case = '{MatchCase}'; Look In Grid = '{GridLookIn}'");
            if (!_searchService.TryFindNext(SearchText, MatchCase, GridLookIn))
            {
                MessageBox.Show(
                    $"The following text was not found: {SearchText}",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void FindPrev()
        {
            _logger.Info($"{nameof(FindPrev)} called with Text = '{SearchText}'; Match Case = '{MatchCase}'; Look In Grid = '{GridLookIn}'");
            if (!_searchService.TryFindPrev(SearchText, MatchCase, GridLookIn))
            {
                MessageBox.Show(
                    $"The following text was not found: {SearchText}",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void FindAll()
        {
            _logger.Info($"{nameof(FindAll)} called with Text = '{SearchText}'; Match Case = '{MatchCase}'; Look In Grid = '{GridLookIn}'");
            var list = _searchService.FindAll(SearchText, MatchCase, GridLookIn);
            FindAllSearchResults = new ObservableCollection<GridPosition>(list);
            if (list.Count == 0)
            {
                MessageBox.Show(
                    $"The following text was not found: {SearchText}",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
