using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ark
{
    public class UploadDocumentsModel
    {
        public UploadDocumentsModel()
        {
            AddFilesToQueue = new RelayCommand((_) => AddFiles(), (_) => CanAddFiles());
        }

        public ICommand AddFilesToQueue { get; }

        private async Task<List<long>> AddFiles()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Документы Word|*.docx, *.doc";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() != true)
                return new List<long>();
            var docs = new List<Models.Document>();
            foreach (string fileName in dialog.FileNames)
            {
                docs.Add(new Models.Document()
                {
                    Bytes = File.ReadAllBytes(fileName),
                    Name = Path.GetFileName(fileName),
                });
            }
            return await DatabaseContext.Create(docs);
        }

        private bool CanAddFiles()
        {
            return true;
        }
    }
}
