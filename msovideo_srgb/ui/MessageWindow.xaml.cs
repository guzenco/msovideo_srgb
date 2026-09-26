using System.Windows;
using System.Windows.Input;

namespace msovideo_srgb
{
    public partial class MessageWindow : Window
    {
        public string Message
        {
            get => MessageTextBlock.Text;
            set => MessageTextBlock.Text = value;
        }

        public Window ParentWindow { get; set; }

        public MessageWindow()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var parent = ParentWindow ?? Owner;
            if (parent != null)
            {
                Left = parent.Left + (parent.ActualWidth - ActualWidth) / 2;
                Top = parent.Top + (parent.ActualHeight - ActualHeight) / 2;
            }
        }
    }
}
