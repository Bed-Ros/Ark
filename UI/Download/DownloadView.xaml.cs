using System.Windows;

namespace Ark.UI.Download
{
    public partial class DownloadView : Window
    {
        public DownloadView(object context)
        {
            InitializeComponent();
            DataContext = context;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
