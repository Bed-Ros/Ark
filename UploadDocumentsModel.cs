using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Ark
{
    public class UploadDocumentsModel : INotifyPropertyChanged
    {
        public UploadDocumentsModel()
        {
            AddFilesToQueue = new RelayCommand((_) => AddFiles(), (_) => CanAddFiles());
        }

        public ObservableCollection<LoadingDocument> Queue { get; set; } = new ObservableCollection<LoadingDocument>();

        public ICommand AddFilesToQueue { get; }

        string[] ChooseFiles()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Документы Word|*.docx",
                Multiselect = true
            };
            if (dialog.ShowDialog() != true)
                return Array.Empty<string>();
            return dialog.FileNames;
        }

        void AddFiles()
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

        bool canAddFiles { get; set; } = true;
        bool CanAddFiles()
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
