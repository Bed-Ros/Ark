using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Ark
{
    public static class Global
    {
        public static readonly string TempFolderPath;

        static Global()
        {
            TempFolderPath = Path.Combine(AppContext.BaseDirectory, Properties.Settings.Default.TempFolderName);
            Directory.CreateDirectory(TempFolderPath);
        }

        //Простая обертка для вывода ошибок на экран
        public static void ErrorDecorator(Action act)
        {
            try
            {
                act();
            }
            catch (Exception exc)
            {
                ErrorMessageBox(exc.Message);
            }
        }
        public async static Task ErrorDecorator(Func<Task> act)
        {
            try
            {
                await act();
            }
            catch (Exception exc)
            {
                ErrorMessageBox(exc.Message);
            }
        }

        //Сообщение об ошибке 
        public static void ErrorMessageBox(string message)
        {
            MessageBox.Show(message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
