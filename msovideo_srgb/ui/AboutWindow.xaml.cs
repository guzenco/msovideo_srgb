using System.Windows;
using System.Windows.Navigation;
using System.Linq;
using System.Reflection;

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
        public static string Version => string.Join(".", Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.').Take(3));
        public static string Commit => Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "Commit")?.Value;
    }
}