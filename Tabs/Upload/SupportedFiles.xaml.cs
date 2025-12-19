using Ark.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Ark.Tabs.Upload
{
    public partial class SupportedFiles : Window
    {
        public SupportedFiles()
        {
            InitializeComponent();
            FillInTreeView();
        }

        public void FillInTreeView()
        {
            FilesTreeView.Items.Clear();
            Dictionary<FileGroup, List<string>> groups = new();
            foreach (var pair in FilesService.SupportedExtensions)
            {
                if(groups.TryGetValue(pair.Value, out List<string>? cur))
                    cur.Add(pair.Key);
                else
                    groups[pair.Value] = [pair.Key];
            }
            foreach (var pair in groups)
            {
                TreeViewItem parent = new() { Header = FilesService.FileGroupNames[pair.Key] };
                foreach (string item in pair.Value)
                {
                    parent.Items.Add(new TreeViewItem() { Header = item });
                }
                FilesTreeView.Items.Add(parent);
            }
        }
    }
}
