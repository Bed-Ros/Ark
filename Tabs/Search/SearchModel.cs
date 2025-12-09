using Ark.Models;
using Ark.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Ark.Tabs.Search
{
    public class SearchModel : TabModel, INotifyPropertyChanged
    {
        public SearchModel()
        {
            NextPageCommand = new RelayCommand((_) => MoveToNextPage(), (_) => CanMoveToNextPage());
            PreviousPageCommand = new RelayCommand((_) => MoveToPreviousPage(), (_) => CanMoveToPreviousPage());
            SearchCommand = new RelayCommand((_) => LoadCurrentPage());
            DownloadCommand = new RelayCommand((_) => LoadCurrentPage());
        }

        public override void Refresh()
        {
            LoadCurrentPage();
        }

        private ObservableCollection<DbFile> dataItems = new();
        public ObservableCollection<DbFile> DataItems
        {
            get { return dataItems; }
            private set { dataItems = value; OnPropertyChanged(nameof(DataItems)); }
        }

        private int currentPage = 1;
        public int CurrentPage
        {
            get { return currentPage; }
            set { currentPage = value; OnPropertyChanged(nameof(CurrentPage)); LoadCurrentPage(); }
        }

        private int itemsPerPage = Properties.Settings.Default.ItemsPerPage;
        public int ItemsPerPage
        {
            get { return itemsPerPage; }
            set { itemsPerPage = value; OnPropertyChanged(nameof(ItemsPerPage)); LoadCurrentPage(); }
        }

        private long totalItems;
        public long TotalItems
        {
            get { return totalItems; }
            set { totalItems = value; OnPropertyChanged(nameof(TotalItems)); OnPropertyChanged(nameof(TotalPages)); }
        }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / ItemsPerPage);

        private string? searchText;
        public string? SearchText
        {
            get { return searchText; }
            set { searchText = value; OnPropertyChanged(nameof(SearchText)); OnPropertyChanged(nameof(SearchTextIsNotEmpty)); CurrentPage = 1; }
        }

        public bool SearchTextIsNotEmpty => !string.IsNullOrWhiteSpace(SearchText);

        private bool isAllSelected = false;
        public bool IsAllSelected
        {
            get { return isAllSelected; }
            set
            {
                isAllSelected = value;
                OnPropertyChanged(nameof(IsAllSelected));

                foreach (var item in DataItems)
                {
                    item.IsSelected = value;
                }
            }
        }

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand DownloadCommand { get; }

        private async void LoadCurrentPage()
        {
            TotalItems = await DatabaseService.GetDocumentsCount(SearchText);
            DataItems = new ObservableCollection<DbFile>(await DatabaseService.GetDocumentsPage(CurrentPage, SearchText));
        }

        private void MoveToNextPage()
        {
            CurrentPage++;
        }

        private bool CanMoveToNextPage()
        {
            return CurrentPage < TotalPages;
        }

        private void MoveToPreviousPage()
        {
            CurrentPage--;
        }

        private bool CanMoveToPreviousPage()
        {
            return CurrentPage > 1;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
