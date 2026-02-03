using CommunityToolkit.Mvvm.Input;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.UI.Models;
using SSMSMint.ResultsGridSearch.Models;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SSMSMint.ResultsGridSearch.ViewModels
{
    internal class ResultsGridSearchToolWindowViewModel : INotifyPropertyChanged
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

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

        private FindAllSearchResult _findAllSearchResults;
        public FindAllSearchResult FindAllSearchResults
        {
            get => _findAllSearchResults;
            set
            {
                _findAllSearchResults = value;
                OnPropertyChanged(nameof(FindAllSearchResults));
                OnPropertyChanged(nameof(HasFindAllSearchResults));
                OnPropertyChanged(nameof(FindAllSearchResultsCount));
            }
        }

        public bool HasFindAllSearchResults => FindAllSearchResults.GridCells is not null && FindAllSearchResults.GridCells.Count() > 0;
        public int FindAllSearchResultsCount => FindAllSearchResults.GridCells is null ? 0 : FindAllSearchResults.GridCells.Count();

        public GridLookInTypeEn GridLookIn { get; set; }
        public bool MatchCase { get; set; }
        public bool MatchWholeCell { get; set; }

        public AsyncRelayCommand FindAllCommand { get; }
        public AsyncRelayCommand FindNextCommand { get; }
        public AsyncRelayCommand FindPrevCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly IUINotificationManager uiManager;
        private readonly IResultsGridSearchFeature feature;

        public ResultsGridSearchToolWindowViewModel(IUINotificationManager uiManager, IResultsGridSearchFeature feature)
        {
            this.uiManager = uiManager;
            this.feature = feature;

            FindAllCommand = new AsyncRelayCommand(
                execute: FindAllAsync,
                canExecute: () => !string.IsNullOrEmpty(SearchText)
                );
            FindNextCommand = new AsyncRelayCommand(
                execute: FindNextAsync,
                canExecute: () => !string.IsNullOrEmpty(SearchText)
                );
            FindPrevCommand = new AsyncRelayCommand(
                execute: FindPrevAsync,
                canExecute: () => !string.IsNullOrEmpty(SearchText)
                );
        }

        public async void SearchResultItemSelectionChanged(GridCell selectedItem)
        {
            try
            {
                await selectedItem.GridManager.FocusCellAsync(selectedItem.Point);
            }
            catch (Exception ex)
            {
                uiManager.ShowError("Failed to focus on cell", ex.Message);
            }
        }

        private async Task FindNextAsync()
        {
            try
            {
                logger.Info($"{nameof(FindNextAsync)} called with Text = '{SearchText}'; Match Case = '{MatchCase}'; Match Whole Cell = '{MatchWholeCell}'; Look In Grid = '{GridLookIn}'");
                if (!await feature.TryFindNextAsync(SearchText, MatchCase, MatchWholeCell, GridLookIn))
                {
                    uiManager.ShowInfo("Results search", $"Указанный текст не найден: {SearchText}");
                }
            }
            catch (Exception ex)
            {
                uiManager.ShowError("Failed to find next", ex.Message);
            }
        }

        private async Task FindPrevAsync()
        {
            try
            {
                logger.Info($"{nameof(FindPrevAsync)} called with Text = '{SearchText}'; Match Case = '{MatchCase}'; Match Whole Cell = '{MatchWholeCell}'; Look In Grid = '{GridLookIn}'");
                if (!await feature.TryFindPrevAsync(SearchText, MatchCase, MatchWholeCell, GridLookIn))
                {
                    uiManager.ShowInfo("Results search", $"Указанный текст не найден: {SearchText}");
                }
            }
            catch (Exception ex)
            {
                uiManager.ShowError("Failed to find previous", ex.Message);
            }
        }

        private async Task FindAllAsync()
        {
            try
            {
                logger.Info($"{nameof(FindAllAsync)} called with Text = '{SearchText}'; Match Case = '{MatchCase}'; Match Whole Cell = '{MatchWholeCell}'; Look In Grid = '{GridLookIn}'");

                // Чтобы сделать полность асинхронно по нормальному - надо как то по другому работать в feature.FindAll.
                // Потому что там идет обращение к IGridControl и если делать это в фоновой задаче, то будет постоянное переключение между фоновым потоком и UI
                // А это делает поиск в 100 раз медленнее чем синхронный вариант. 
                // Сделаем хоть обманку простую, покажем пользователю что перед новым поиском список очистился.
                // Для этого освободим поток, чтобы UI смог отреагировать и перерисовать список
                FindAllSearchResults.GridCells?.Clear();
                OnPropertyChanged(nameof(FindAllSearchResultsCount));
                await Task.Delay(100);

                var list = feature.FindAll(SearchText, MatchCase, MatchWholeCell, GridLookIn);
                FindAllSearchResults = new(new ObservableCollection<GridCell>(list), SearchText, MatchCase);
                if (list.Count == 0)
                {
                    uiManager.ShowInfo("Results search", $"Указанный текст не найден: {SearchText}");
                }
            }
            catch (Exception ex)
            {
                uiManager.ShowError("Failed to find all", ex.Message);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
