using System.Data.SqlTypes;
using System.Windows.Controls;

namespace Ark
{

    public partial class SearchControl : UserControl
    {
        public SearchControl(object context)
        {
            InitializeComponent();
            DataContext = context;
        }
    }
}
