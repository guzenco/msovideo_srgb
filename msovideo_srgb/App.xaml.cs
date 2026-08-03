using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace msovideo_srgb
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public event EventHandler OnAppExit;

        public static App CurrentApp => Current as App;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("Already running!");
                Shutdown();
                return;
            }

            base.OnStartup(e);

            MainWindow = new MainWindow();

            if (MainWindow.WindowState == WindowState.Normal)
            {
                MainWindow.Show();
            }
        }

        public void OnExit()
        {
            OnAppExit?.Invoke(null, null);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            OnExit();
        }
    }
}