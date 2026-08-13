using System.Windows;
using System.Windows.Controls;

namespace msovideo_srgb
{
    public partial class AdvancedWindow
    {
        private AdvancedViewModel _viewModel;

        public AdvancedWindow(MonitorData monitor)
        {
            _viewModel = new AdvancedViewModel(monitor);
            DataContext = _viewModel;
            InitializeComponent();
        }

        private static string BrowseProfiles()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ICC Profiles|*.icc;*.icm"
            };

            var result = dlg.ShowDialog();

            return result == true ? dlg.FileName : null;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var profilePath = BrowseProfiles();
            if (!string.IsNullOrEmpty(profilePath))
            {
                _viewModel.ProfilePath = profilePath;
            }
        }

        private void Browse_Click_HDR(object sender, RoutedEventArgs e)
        {
            var profilePath = BrowseProfiles();
            if (!string.IsNullOrEmpty(profilePath))
            {
                _viewModel.ProfilePathHDR = profilePath;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ApplyChanges();
            DialogResult = true;
        }

        public string[] ChangedProperties => _viewModel.ChangedProperties.ToArray();
    }
}