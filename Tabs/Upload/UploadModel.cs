using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Ark.Tabs.Upload
{
    public class UploadModel : TabModel, INotifyPropertyChanged
    {
        public UploadModel()
        {
            AddFilesToQueue = new RelayCommand((_) => AddFiles(), (_) => CanAddFiles());
        }

        public ObservableCollection<LoadingDocument> Queue { get; set; } = new();

        public ICommand AddFilesToQueue { get; }

        static string[] ChooseFiles()
        {
            var dialog = new OpenFileDialog { Multiselect = true };
            if (dialog.ShowDialog() != true)
                return Array.Empty<string>();
            return dialog.FileNames;
        }

        private void AddFiles()
        {
            try
            {
                canAddFiles = false;
                foreach (string fileName in ChooseFiles())
                {
                    var cur = new LoadingDocument(fileName);
                    _ = cur.Load();
                    Queue.Add(cur);
                }
            }
            finally
            {
                canAddFiles = true;
            }
        }

        private bool canAddFiles = true;
        private bool CanAddFiles()
        {
            return canAddFiles;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
