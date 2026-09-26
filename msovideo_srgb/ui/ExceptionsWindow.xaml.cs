using System.Windows;
using System.Windows.Input;

namespace msovideo_srgb
{
    public partial class ExceptionsWindow : Window
    {
        public string Text { get; }

        public Window OwnerWindow { get; set; }

        public ExceptionsWindow(string text)
        {
            Text = text;
            DataContext = this;
            InitializeComponent();
        }

        private bool _isClosed = false;
        public void SafeClose()
        {
            if (_isClosed) return;

            _isClosed = true;
            Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SafeClose();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SafeClose();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (OwnerWindow != null)
            {
                Left = OwnerWindow.Left + (OwnerWindow.ActualWidth - ActualWidth) / 2;
                Top = OwnerWindow.Top + (OwnerWindow.ActualHeight - ActualHeight) / 2;
            }
        }
    }
}
