using Ark.Models;
using Ark.Services;
using Ark.Tabs;
using Ark.UI.Download;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace Ark.UI.Search
{
    public class SearchModel : TabModel, INotifyPropertyChanged
    {
        public SearchModel()
        {
            NextPageCommand = new RelayCommand((_) => MoveToNextPage(), (_) => CanMoveToNextPage());
            PreviousPageCommand = new RelayCommand((_) => MoveToPreviousPage(), (_) => CanMoveToPreviousPage());
            SearchCommand = new RelayCommand((_) => LoadCurrentPage());
            OpenSelectedFileCommand = new RelayCommand((_) => OpenSelectedFile(), (_) => FileIsSelected());
            DownloadFilesCommand = new RelayCommand((_) => DownloadCheckedFiles(), (_) => AnyFileChecked());
        }

        //Список возможного количества файлов на странице
        public static ObservableCollection<int> PosibleFilesPerPage => [10, 50, 100, 500, 1000];

        //Все отмеченные на данный момент файлы
        private readonly Dictionary<long, DbFile> checkedFiles = [];

        //Файлы
        private ObservableCollection<DbFile> files = [];
        public ObservableCollection<DbFile> Files
        {
            get { return files; }
            private set
            {
                files = value;
                OnPropertyChanged(nameof(Files));
            }
        }

        //Источники
        private ObservableCollection<Source> sources = [];
        public ObservableCollection<Source> Sources
        {
            get { return sources; }
            private set
            {
                sources = value;
                OnPropertyChanged(nameof(Sources));
            }
        }

        //Номер текущей страницы
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

        //Количество файлов на странице
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

        //Всего файлов в БД
        private long totalFiles;
        public long TotalFiles
        {
            get { return totalFiles; }
            set
            {
                totalFiles = value;
                OnPropertyChanged(nameof(TotalFiles));
                OnPropertyChanged(nameof(TotalPages));
            }
        }

        //Всего "страниц в БД"
        public int TotalPages => (int)Math.Ceiling((double)TotalFiles / FilesPerPage);

        //Текст поиска файлов
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

        //Все ли файлы на текущей странице отмечены
        private bool isAllChecked = false;
        public bool IsAllChecked
        {
            get { return isAllChecked; }
            set
            {
                for (int i = 0; i < Files.Count; i++)
                {
                    Files[i].IsChecked = value;
                }
                isAllChecked = value;
                OnPropertyChanged(nameof(IsAllChecked));
            }
        }

        //Происходит ли загрузка
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

        //Нужно ли искать в тексте файлов
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

        //Выбранный файл
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

        //Выбранный источник
        private Source? selectedSource;
        public Source? SelectedSource
        {
            get { return selectedSource; }
            set
            {
                selectedSource = value;
                OnPropertyChanged(nameof(SelectedSource));
                CurrentPage = 1;
            }
        }

        //Команды
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand OpenSelectedFileCommand { get; }
        public ICommand DownloadFilesCommand { get; }

        //Обновление закладки
        public override void Refresh() => LoadCurrentPage();

        //Отмечен ли любой файл на странице
        private bool AnyFileChecked() => checkedFiles.Count != 0;

        //Выбран ли любой файл на странице
        private bool FileIsSelected() => SelectedFile is not null;

        //Скачивание отмеченных файлов
        private void DownloadCheckedFiles()
        {
            Global.ErrorDecorator(() =>
            {
                new DownloadView(new DownloadModel(checkedFiles.Values)).ShowDialog();
            });
        }

        //TODO использовать? Отметка всех файлов соответствующий фильтрам
        private async void CheckAllFilteredFiles(bool check)
        {
            try
            {
                IsLoading = true;
                var filter = new FileFilter() { SearchInText = SearchInFileText, Text = SearchText, SourceId = SelectedSource?.Id };
                if (check)
                {
                    var files = await DatabaseService.GetFiles(filter);
                    foreach (var file in files)
                    {
                        checkedFiles[file.Id] = file;
                    }
                }
                else
                {
                    foreach (var id in DatabaseService.GetAllFilesIds(filter))
                    {
                        checkedFiles.Remove(id);
                    }
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

        //Обновляем IsAllChecked
        private void UpdateIsAllChecked()
        {
            isAllChecked = true;
            foreach (DbFile f in Files)
            {
                if (!checkedFiles.ContainsKey(f.Id))
                {
                    isAllChecked = false;
                    break;
                }
            }
            OnPropertyChanged(nameof(IsAllChecked));
        }

        //Загрузка текущей страницы
        private async void LoadCurrentPage()
        {
            try
            {
                IsLoading = true;
                //Источники
                List<Source> sources = [new Source(), .. await DatabaseService.GetAllSources()];
                Sources = new ObservableCollection<Source>(sources);
                //Файлы
                var filter = new FileFilter() { SearchInText = SearchInFileText, Text = SearchText, SourceId = SelectedSource?.Id };
                if (filter.SourceId == 0) filter.SourceId = null;
                TotalFiles = await DatabaseService.GetAllFilesCount(filter);
                Files = new ObservableCollection<DbFile>(await DatabaseService.GetFilesPage(CurrentPage, filter));
                foreach (var file in Files)
                {
                    file.PropertyChanged += File_PropertyChanged;
                    if (checkedFiles.ContainsKey(file.Id))
                        file.IsChecked = true;
                }
                UpdateIsAllChecked();
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

        //Update файла в БД и обработка отметки файла
        private async void File_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            await Global.ErrorDecorator(async () =>
            {
                if (sender is null) return;
                var file = (DbFile)sender;
                switch (e.PropertyName)
                {
                    case nameof(DbFile.Name):
                        await DatabaseService.Update(file, e.PropertyName);
                        break;
                    case nameof(DbFile.IsChecked):
                        //Сохраняем выбор
                        if (file.IsChecked)
                            checkedFiles[file.Id] = file;
                        else
                            checkedFiles.Remove(file.Id);
                        //Обновляем галку "выбрать все"
                        UpdateIsAllChecked();
                        break;
                    default:
                        break;
                }
            });
        }

        //Открытие выбранного файла
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

        //Переходы между страницами и их условия
        private void MoveToNextPage() => CurrentPage++;
        private bool CanMoveToNextPage() => CurrentPage < TotalPages;
        private void MoveToPreviousPage() => CurrentPage--;
        private bool CanMoveToPreviousPage() => CurrentPage > 1;

        //PropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
