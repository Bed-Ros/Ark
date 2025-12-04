using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Ark
{
    public class LoadingDocument : INotifyPropertyChanged
    {
        string name = "";
        public string Name
        {
            get { return name; }
            private set { name = value; OnPropertyChanged(nameof(Name)); }
        }
        string status = "";
        public string Status
        {
            get { return status; }
            private set { status = value; OnPropertyChanged(nameof(Status)); }
        }
        Exception? exception;
        public Exception? Exception
        {
            get { return exception; }
            private set { exception = value; OnPropertyChanged(nameof(Exception)); }
        }
        readonly string Filepath;
        public LoadingDocument(string filePath)
        {
            Filepath = filePath;
        }

        public async Task Load()
        {
            try
            {
                Status = "Подготовка";
                Name = Path.GetFileName(Filepath);
                var doc = new Models.Document()
                {
                    Bytes = File.ReadAllBytes(Filepath),
                    Name = Name,
                };
                Status = "Загрузка";
                await DatabaseContext.Create(doc);
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
