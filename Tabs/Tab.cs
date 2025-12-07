using System.Windows.Controls;

namespace Ark.Tabs
{
    public struct Tab
    {
        public string Header;
        public ITabModel Model;
        public UserControl Control;
    }
}
