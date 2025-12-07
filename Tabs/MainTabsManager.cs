using Ark.Tabs.Search;
using Ark.Tabs.Upload;
using System.Collections.ObjectModel;

namespace Ark.Tabs
{
    public class MainTabsManager
    {
        public MainTabsManager()
        {
            Tabs = new()
            {
                new Tab()
                {
                    Header = "Поиск",
                    Control = new SearchControl(),
                    Model = new SearchModel(),
                },
                new Tab()
                {
                    Header = "Загрузка",
                    Control = new UploadControl(),
                    Model = new UploadModel(),
                },
            };
        }

        public readonly ObservableCollection<Tab> Tabs;

        private Tab selectedTab;
        public Tab SelectedTab
        {
            get => selectedTab;
            set
            {
                selectedTab = value;
                selectedTab.Model.Refresh();
            }
        }
    }
}
