using Ark.Models;
using Ark.Services;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Ark
{
    public class LoadingFile : INotifyPropertyChanged
    {
        string filepath = "";
        public string Filepath
        {
            get { return filepath; }
            private set
            {
                filepath = value;
                OnPropertyChanged(nameof(Filepath));
            }
        }

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

        public LoadingFile(string filePath)
        {
            Filepath = filePath;
        }

        public async Task Load()
        {
            try
            {                
                Status = "Подготовка";
                DbFile file = await FilesService.ReadFile(Filepath);
                Status = "Загрузка";
                await DatabaseService.Create(file);
                Status = "Готово";
            }
            catch (Exception e)
            {
                Status = "Ошибка";
                Exception = e;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
