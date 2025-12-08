using System.Windows.Controls;

namespace Ark.Tabs
{
    public struct Tab
    {
        public string Header { get; set; }
        public TabModel Model { get; set; }
        public UserControl Control { get; set; }
    }
}
