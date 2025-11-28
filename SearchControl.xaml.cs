using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ark
{
    public partial class SearchControl : UserControl
    {
        public SearchControl()
        {
            NextPageCommand = new RelayCommand((_) => MoveToNextPage(), (_)=> CanMoveToNextPage());
            PreviousPageCommand = new RelayCommand((_) => MoveToPreviousPage(), (_) => CanMoveToPreviousPage());
            LoadData(); // Initial data load
            InitializeComponent();
        }

        private ObservableCollection<MyDataItem> _dataItems;
        public ObservableCollection<MyDataItem> DataItems
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

        private int _itemsPerPage = 10;
        public int ItemsPerPage
        {
            get { return _itemsPerPage; }
            set { _itemsPerPage = value; OnPropertyChanged(nameof(ItemsPerPage)); LoadCurrentPage(); }
        }

        private int _totalItems;
        public int TotalItems
        {
            get { return _totalItems; }
            set { _totalItems = value; OnPropertyChanged(nameof(TotalItems)); OnPropertyChanged(nameof(TotalPages)); }
        }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / ItemsPerPage);

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        private void LoadData()
        {
            // Simulate fetching total count from database
            TotalItems = 100; // Example
            LoadCurrentPage();
        }

        private void LoadCurrentPage()
        {
            // Simulate fetching paged data from database
            // In a real application, you'd make a database call with OFFSET and FETCH NEXT
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
            if (CanMoveToNextPage())
            {
                CurrentPage++;
            }
        }

        private bool CanMoveToNextPage()
        {
            return CurrentPage < TotalPages;
        }

        private void MoveToPreviousPage()
        {
            if (CanMoveToPreviousPage())
            {
                CurrentPage--;
            }
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
