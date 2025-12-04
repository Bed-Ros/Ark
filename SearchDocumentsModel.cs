using Ark.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Ark
{
    public class SearchDocumentsModel : INotifyPropertyChanged
    {
        public SearchDocumentsModel()
        {
            NextPageCommand = new RelayCommand((_) => MoveToNextPage(), (_) => CanMoveToNextPage());
            PreviousPageCommand = new RelayCommand((_) => MoveToPreviousPage(), (_) => CanMoveToPreviousPage());
            LoadData();
        }

        private ObservableCollection<Document> _dataItems;
        public ObservableCollection<Document> DataItems
        {
            get { return _dataItems; }
            set { _dataItems = value; OnPropertyChanged(nameof(DataItems)); }
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

        private void LoadData()
        {
            TotalItems = DatabaseContext.GetDocumentsCount();
            LoadCurrentPage();
        }

        private void LoadCurrentPage()
        {
            var startIndex = (CurrentPage - 1) * ItemsPerPage;
            var endIndex = Math.Min(startIndex + ItemsPerPage, TotalItems);

            var simulatedData = new ObservableCollection<MyDataItem>();
            for (int i = startIndex; i < endIndex; i++)
            {
                simulatedData.Add(new MyDataItem { Id = i + 1, Name = $"Item {i + 1}" });
            }
            DataItems = simulatedData;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
