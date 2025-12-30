using System.Windows.Controls;

namespace Ark.UI.Search
{
    public partial class SearchView : UserControl
    {
        public SearchView(object context)
        {
            InitializeComponent();
            DataContext = context;
        }
    }
}
