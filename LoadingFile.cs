using Ark.Models;
using Ark.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ark
{
    public class LoadingFile : INotifyPropertyChanged
    {
        //Конструктор для скачивания
        public LoadingFile(DbFile file, string? folderPath)
        {
            File = file;

            if (string.IsNullOrWhiteSpace(folderPath)) return;
            var rootPart = System.IO.Path.GetPathRoot(File.FullPath);
            var secondPart = File.FullPath[(rootPart ?? "").Length..];
            if(secondPart.StartsWith('/') || secondPart.StartsWith('\\')) 
                secondPart = secondPart[1..];
            Path = System.IO.Path.Combine(folderPath, secondPart);
        }

        //Конструктор для загрузки
        public LoadingFile(string path)
        {
            Path = path;
        }

        //Путь скачивания/загрузки
        string path = null!;
        public string Path
        {
            get { return path; }
            private set
            {
                path = value;
                OnPropertyChanged(nameof(Path));
            }
        }

        //Файл
        public DbFile? File { get; set; }

        //Статус
        string status = "";
        public string Status
        {
            get { return status; }
            private set
            {
                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        //Исключение при выполнении
        Exception? exception;
        public Exception? Exception
        {
            get { return exception; }
            private set
            {
                exception = value;
                OnPropertyChanged(nameof(Exception));
            }
        }

        //Скачивание
        public async Task Download(bool overwrite)
        {
            try
            {
                if (Path is null) throw new ArgumentNullException(nameof(Path));
                if (File is null) throw new ArgumentNullException(nameof(File));
                Status = "Подготовка";
                var existed = System.IO.Path.Exists(Path);
                if (!overwrite && existed)
                {
                    Status = "Файл уже существует";
                    return;
                }
                Status = "Скачивание";
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path) ?? "");
                await DatabaseService.DownloadFile(File.Id, Path);
                if (existed)
                    Status = "Перезаписан";
                else
                    Status = "Скачан";
            }
            catch (Exception e)
            {
                Status = "Ошибка";
                Exception = e;
            }
        }

        //Загрузка
        public async Task Upload()
        {
            try
            {
                if (Path is null) throw new ArgumentNullException(nameof(Path));
                Status = "Подготовка";
                DbFile file = await FilesService.ReadFile(Path);
                Status = "Загрузка";
                var upsertResult = await DatabaseService.Create(file);
                if (upsertResult.Action.Equals(nameof(AuditState.Insert), StringComparison.InvariantCultureIgnoreCase))
                    Status = "Создано";
                else
                    Status = "Перезаписано";
            }
            catch (Exception e)
            {
                Status = "Ошибка";
                Exception = e;
            }
        }

        //PropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
