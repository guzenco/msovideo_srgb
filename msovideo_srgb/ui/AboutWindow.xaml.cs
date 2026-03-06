using System.Windows;
using System.Windows.Navigation;
using System.Linq;

namespace msovideo_srgb
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(processStartInfo);
        }
        public static string Version => string.Join(".", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.').Take(3));
    }
}