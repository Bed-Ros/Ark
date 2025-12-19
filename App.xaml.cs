using System.IO;
using System.Windows;

namespace Ark
{
    public partial class App : Application
    {
        //Удаляем временные файлы после закрытия программы
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Global.ErrorDecorator(() =>
            {
                Directory.Delete(Global.TempFolderPath, true);
            });
        }
    }
}
