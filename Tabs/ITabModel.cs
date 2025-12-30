using System.Threading.Tasks;
using System.Windows.Controls;

namespace Ark.Tabs
{
    public abstract class TabModel : UserControl
    {
        public virtual void Refresh() { }
    }
}
