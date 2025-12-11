using Ark.Models;
using Ark.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
            OpenSelectedFileCommand = new RelayCommand((_) => OpenSelectedFile());
        }

        public override void Refresh()
        {
            LoadCurrentPage();
        }

        private ObservableCollection<DbFile> files = new();
        public ObservableCollection<DbFile> Files
        {
            get { return files; }
            private set
            {
                files = value;
                OnPropertyChanged(nameof(Files));
            }
        }

        private int currentPage = 1;
        public int CurrentPage
        {
            get { return currentPage; }
            set
            {
                currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
                LoadCurrentPage();
            }
        }

        public static ObservableCollection<int> PosibleItemsPerPage => new() { 10, 50, 100, 500, 1000 };
        public int FilesPerPage
        {
            get { return Properties.Settings.Default.ItemsPerPage; }
            set
            {
                Properties.Settings.Default.ItemsPerPage = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(FilesPerPage));
                OnPropertyChanged(nameof(TotalPages));
                LoadCurrentPage();
            }
        }

        private long totalItems;
        public long TotalFiles
        {
            get { return totalItems; }
            set
            {
                totalItems = value;
                OnPropertyChanged(nameof(TotalFiles));
                OnPropertyChanged(nameof(TotalPages));
            }
        }

        public int TotalPages => (int)Math.Ceiling((double)TotalFiles / FilesPerPage);

        private string? searchText;
        public string? SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                OnPropertyChanged(nameof(SearchText));
                OnPropertyChanged(nameof(SearchTextIsNotEmpty));
                CurrentPage = 1;
            }
        }

        public bool SearchTextIsNotEmpty => !string.IsNullOrWhiteSpace(SearchText);

        private bool isAllChecked = false;
        public bool IsAllSelected
        {
            get { return isAllChecked; }
            set
            {
                isAllChecked = value;
                OnPropertyChanged(nameof(IsAllSelected));

                foreach (var item in Files)
                {
                    item.IsSelected = value;
                }
            }
        }

        private DbFile? selectedFile;
        public DbFile? SelectedFile
        {
            get { return selectedFile; }
            set
            {
                selectedFile = value;
                OnPropertyChanged(nameof(SelectedFile));
            }
        }

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand OpenSelectedFileCommand { get; }

        private async void LoadCurrentPage()
        {
            TotalFiles = await DatabaseService.GetAllFilesCount(SearchText);
            Files = new ObservableCollection<DbFile>(await DatabaseService.GetFilesPage(CurrentPage, SearchText));
            foreach (var file in Files)
            {
                file.PropertyChanged += File_PropertyChanged;
            }
        }

        private async void File_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DbFile.Name) || sender is null)
                return;
            await DatabaseService.Update((DbFile)sender, e.PropertyName);
        }

        private async void OpenSelectedFile()
        {
            if (SelectedFile is null) return;
            DbFile file = await DatabaseService.GetFile(SelectedFile.Id, true);
            string path = Path.Combine(Global.TempFolderPath, file.Name + file.Extension);
            await File.WriteAllBytesAsync(path, file.Bytes);
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
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
