using Ark.Models;
using Ark.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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

        public static ObservableCollection<int> PosibleItemsPerPage => [10, 50, 100, 500, 1000];
        public int FilesPerPage
        {
            get { return Properties.Settings.Default.ItemsPerPage; }
            set
            {
                Global.ErrorDecorator(() =>
                {
                    Properties.Settings.Default.ItemsPerPage = value;
                    Properties.Settings.Default.Save();
                    OnPropertyChanged(nameof(FilesPerPage));
                    OnPropertyChanged(nameof(TotalPages));
                    CurrentPage = 1;
                });
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
                CurrentPage = 1;
            }
        }

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

        private bool isLoading = false;
        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private bool searchInFileText = false;
        public bool SearchInFileText
        {
            get { return searchInFileText; }
            set
            {
                searchInFileText = value;
                OnPropertyChanged(nameof(SearchInFileText));
                CurrentPage = 1;
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
            try
            {
                IsLoading = true;
                TotalFiles = await DatabaseService.GetAllFilesCount(SearchInFileText, SearchText);
                Files = new ObservableCollection<DbFile>(await DatabaseService.GetFilesPage(CurrentPage, SearchInFileText, SearchText));
                foreach (var file in Files)
                {
                    file.PropertyChanged += File_PropertyChanged;
                }
            }
            catch (Exception exc)
            {
                Global.ErrorMessageBox(exc.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void File_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            await Global.ErrorDecorator(async () =>
            {
                if (e.PropertyName != nameof(DbFile.Name) || sender is null)
                    return;
                await DatabaseService.Update((DbFile)sender, e.PropertyName);
            });
        }

        private async void OpenSelectedFile()
        {
            try
            {
                IsLoading = true;            
                if (SelectedFile is null) return;
                string path = Path.Combine(Global.TempFolderPath, SelectedFile.Name + SelectedFile.Extension);
                if (!await DatabaseService.DownloadFile(SelectedFile.Id, path)) return;
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception exc)
            {
                Global.ErrorMessageBox(exc.Message);
            }
            finally
            {
                IsLoading = false;
            }
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
