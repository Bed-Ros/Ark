using System.Windows.Controls;

namespace Ark
{
    public partial class UploadControl : UserControl
    {
        public UploadControl(object context)
        {
            InitializeComponent();
            DataContext = context;
        }
    }
}
