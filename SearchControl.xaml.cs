using System.Windows.Controls;

namespace Ark
{
    public partial class SearchControl : UserControl
    {
        public SearchControl()
        {
            InitializeComponent();
            DataContext = new SearchDocumentsModel();
        }
    }
}
