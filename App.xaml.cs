using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FlashAudio
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                string path = @"Log\" + string.Format("{0}_{1:dd.MM.yyy}.log", AppDomain.CurrentDomain.FriendlyName, DateTime.Now);
                if (!Directory.Exists(@"Log\"))
                    Directory.CreateDirectory(@"Log\");
                using (StreamWriter stream = new StreamWriter(path, true))
                {
                    stream.WriteLine("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3} \n Операционная система: {4} \n Имеет 64-bit? — {6} \n Версия, используемой для программы, .NET Framework: {5} \r\n",
                        DateTime.Now,
                        e.Exception.TargetSite.DeclaringType,
                        e.Exception.TargetSite.Name,
                        e.Exception,
                        Environment.OSVersion,
                        Environment.Version.ToString(),
                        Environment.Is64BitOperatingSystem ? "Да" : "Нет");
                }
            }
            catch { }
            MessageBox.Show(
                $"Исключение {e.Exception.GetType().ToString()} отключено. {Environment.NewLine}Причина: {e.Exception.Message} {Environment.NewLine}Подробности в лог-файле.",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int cmdShow);
        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr handle);

        private readonly Mutex mutex = new Mutex(false, "FlashAudio");

        private void App_Startup(object sender, StartupEventArgs e)
        {
            if (!mutex.WaitOne(500, false))
            {
                string processName = Process.GetCurrentProcess().ProcessName;
                Process process = Process.GetProcesses().Where(p => p.ProcessName == processName).FirstOrDefault();
                if (process != null)
                {
                    IntPtr handle = process.MainWindowHandle;
                    ShowWindow(handle, 1);
                    SetForegroundWindow(handle);
                }
                Current.Shutdown();
                return;
            }
            string fileName = e.Args?.FirstOrDefault();
            MainWindow mainWindow;
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                List<string> vs = new List<string>();
                vs.Add(fileName);
                mainWindow = new MainWindow(vs.ToArray());
            }
            else
                mainWindow = new MainWindow();

            mainWindow.Show();
        }
    }
}
