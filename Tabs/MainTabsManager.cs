using Ark.Tabs.Search;
using Ark.Tabs.Upload;
using System.Collections.ObjectModel;

namespace Ark.Tabs
{
    public class MainTabsManager
    {
        public MainTabsManager()
        {
            var searchModel = new SearchModel();
            var updateModel = new UploadModel();

            Tabs = new()
                {
                    new Tab()
                    {
                        Header = "Поиск",
                        Control = new SearchControl(searchModel),
                        Model = searchModel,
                    },
                    new Tab()
                    {
                        Header = "Загрузка",
                        Control = new UploadControl(updateModel),
                        Model = updateModel,
                    },
                };
        }

        public ObservableCollection<Tab> Tabs { get; private set; }

        private int selectedIndex = -1; //-1 - для обновления первой вкладки
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                selectedIndex = value;
                Tabs[selectedIndex].Model.Refresh();
            }
        }
    }
}
