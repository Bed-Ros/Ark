using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ark.Tabs.Upload
{
    public class UploadModel : TabModel, INotifyPropertyChanged
    {
        public UploadModel()
        {
            AddFilesToQueue = new RelayCommand(async (_) => await AddToQueue(ChooseFiles), (_) => CanAddFiles());
            AddFoldersToQueue = new RelayCommand(async (_) => await AddToQueue(ChooseFoldersFiles), (_) => CanAddFiles());
            ViewSupportedFiles = new RelayCommand((_) => ShowSupportedFiles());
        }

        public ObservableCollection<LoadingFile> Queue { get; set; } = [];

        public ICommand AddFilesToQueue { get; }
        public ICommand AddFoldersToQueue { get; }
        public ICommand ViewSupportedFiles { get; }

        static string[] ChooseFiles()
        {
            var dialog = new OpenFileDialog { Multiselect = true };
            if (dialog.ShowDialog() != true)
                return [];
            return dialog.FileNames;
        }

        static List<string> ChooseFoldersFiles()
        {
            var result = new List<string>();
            var dialog = new OpenFolderDialog { Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                foreach (string directory in dialog.FolderNames)
                {
                    result.AddRange(Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories));
                }
            }
            return result;
        }

        private async Task AddToQueue(Func<IEnumerable<string>> getPaths)
        {
            try
            {
                canAddFiles = false;
                List<LoadingFile> temp = [];
                foreach (string path in getPaths())
                {
                    temp.Add(new LoadingFile(path));
                }
                foreach (LoadingFile f in temp)
                {
                    Queue.Add(f);
                }
                canAddFiles = true;
                await Parallel.ForEachAsync(temp, async (f, _) =>
                {
                    await f.Load();
                });
            }
            catch (Exception exc)
            {
                Global.ErrorMessageBox(exc.Message);
            }
            finally
            {
                canAddFiles = true;
            }            
        }

        private bool canAddFiles = true;
        private bool CanAddFiles() => canAddFiles;

        private static void ShowSupportedFiles()
        {
            Global.ErrorDecorator(() =>
            {
                new SupportedFiles().ShowDialog();
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
