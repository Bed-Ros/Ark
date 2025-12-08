using Ark.Models;
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
        }

        public override void Refresh()
        {
            LoadCurrentPage();
        }

        private ObservableCollection<Document> _dataItems = new ObservableCollection<Document>();
        public ObservableCollection<Document> DataItems
        {
            get { return _dataItems; }
            private set { _dataItems = value; OnPropertyChanged(nameof(DataItems)); }
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get { return _currentPage; }
            set { _currentPage = value; OnPropertyChanged(nameof(CurrentPage)); LoadCurrentPage(); }
        }

        private int _itemsPerPage = Properties.Settings.Default.ItemsPerPage;
        public int ItemsPerPage
        {
            get { return _itemsPerPage; }
            set { _itemsPerPage = value; OnPropertyChanged(nameof(ItemsPerPage)); LoadCurrentPage(); }
        }

        private long _totalItems;
        public long TotalItems
        {
            get { return _totalItems; }
            set { _totalItems = value; OnPropertyChanged(nameof(TotalItems)); OnPropertyChanged(nameof(TotalPages)); }
        }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / ItemsPerPage);

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        private async void LoadCurrentPage()
        {
            TotalItems = DatabaseService.GetDocumentsCount();
            DataItems = new ObservableCollection<Document>(await DatabaseService.GetDocumentsPage(CurrentPage));
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
