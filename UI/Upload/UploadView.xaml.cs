using System.Windows.Controls;

namespace Ark.UI.Upload
{
    public partial class UploadView : UserControl
    {
        public UploadView(object context)
        {
            InitializeComponent();
            DataContext = context;
        }
    }
}
