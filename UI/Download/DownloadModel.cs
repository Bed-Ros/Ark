using Ark.Models;
using Ark.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Ark.UI.Download
{
    public class DownloadModel : INotifyPropertyChanged
    {
        public DownloadModel(IEnumerable<DbFile> files)
        {
            Files = files;
            DownloadCommand = new RelayCommand(async(_) => await Download(), (_) => CanDownload());
            CancelCommand = new RelayCommand(Cancel, (_) => CanCancel());
            ChooseDownloadFolderCommand = new RelayCommand((_) => ChooseDownloadFolder(), (_) => CanChooseDownloadFolder());

            FillQueue();
        }

        //Команды
        public ICommand DownloadCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ChooseDownloadFolderCommand { get; }

        //Переменные
        private readonly IEnumerable<DbFile> Files;

        private ObservableCollection<LoadingFile> queue = [];
        public ObservableCollection<LoadingFile> Queue
        {
            get { return queue; }
            set
            {
                queue = value;
                OnPropertyChanged(nameof(Queue));
            }
        }

        //Папка загрузки
        private string? downloadFolder;
        public string? DownloadFolder
        {
            get { return downloadFolder; }
            set
            {
                downloadFolder = value;
                FillQueue();
                OnPropertyChanged(nameof(DownloadFolder));
            }
        }

        //Происходит ли скачивание
        private bool isDownloading = false;
        public bool IsDownloading
        {
            get { return isDownloading; }
            set
            {
                isDownloading = value;
                OnPropertyChanged(nameof(IsDownloading));
                OnPropertyChanged(nameof(AllowChangeSettings));
            }
        }

        //Можно ли менять настройки
        public bool AllowChangeSettings
        {
            get { return !isDownloading; }
        }

        //Информация для progressbra'а
        private long maxProgress = 100;
        public long MaxProgress
        {
            get { return maxProgress; }
            set
            {
                maxProgress = value;
                OnPropertyChanged(nameof(MaxProgress));
            }
        }

        private long progress = 0;
        public long Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        //Перезапись файлов
        private bool overwrite = false;
        public bool Overwrite
        {
            get { return overwrite; }
            set
            {
                overwrite = value;
                OnPropertyChanged(nameof(Overwrite));
            }
        }

        //Отмена
        private bool cancel = false;
        private void Cancel(object? windowClosingArgs)
        {
            var answer = MessageBox.Show(
                "Вы уверены что хотите отменить скачивание файлов? Уже скачанные файлы не будут удалены.",
                "Отмена",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer == MessageBoxResult.Yes)
                cancel = true;
            else if (windowClosingArgs is not null and CancelEventArgs)
                ((CancelEventArgs)windowClosingArgs).Cancel = true;
        }
        private bool CanCancel()
        {
            return IsDownloading;
        }

        //Заполнение списка скачиваемых файлов
        private void FillQueue()
        {
            List<LoadingFile> temp = [];
            foreach (var file in Files)
            {
                temp.Add(new LoadingFile(file, DownloadFolder));
            }
            Queue = new ObservableCollection<LoadingFile>(temp);
        }

        //Выбор папки загрузки
        private void ChooseDownloadFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() != true) return;
            DownloadFolder = dialog.FolderName;
        }

        private bool CanChooseDownloadFolder()
        {
            return !IsDownloading;
        }

        //Скачивание
        private async Task Download()
        {
            try
            {
                IsDownloading = true;
                Progress = 0;
                MaxProgress = Files.Count();

                foreach (var file in Queue)
                {
                    await file.Download(Overwrite);
                    Progress++;
                    if (cancel) break;
                }
            }
            catch (Exception exc)
            {
                Global.ErrorMessageBox(exc.Message);
            }
            finally
            {
                IsDownloading = false;
            }
        }

        private bool CanDownload()
        {
            return !IsDownloading && !string.IsNullOrWhiteSpace(DownloadFolder);
        }

        //PropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
