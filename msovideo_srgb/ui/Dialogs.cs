using System.Windows.Controls;
using System.Windows;

namespace msovideo_srgb
{
    public static class Dialogs
    {
        public static string InputDialog(string text, string title = null)
        {
            Window dialog = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            TextBox textBox = new TextBox { Text = text, Padding = new Thickness(1, 1, 1, 1)};

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            Button okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 6, 0), IsDefault = true };
            Button cancelButton = new Button { Content = "Cancel", Width = 75, Margin = new Thickness(0, 0, 0, 0), IsCancel = true};

            okButton.Click += (s, e) =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            };

            dialog.Loaded += (s, e) =>
            {
                textBox.Focus();
                textBox.SelectAll();
            };

            panel.Children.Add(textBox);
            
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            panel.Children.Add(buttons);

            dialog.Content = panel;

            if (dialog.ShowDialog() == true)
            {
                return textBox.Text;
            }

            return null;
        }

        public static bool ConfirmDialog(string text, string title = null)
        {
            Window dialog = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            Label label = new Label { Content = text, Padding = new Thickness(1, 1, 1, 1) };
            
            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            Button okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 6, 0), IsDefault = true };
            Button cancelButton = new Button { Content = "Cancel", Width = 75, IsCancel = true };

            okButton.Click += (s, e) =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            };

            panel.Children.Add(label);

            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            panel.Children.Add(buttons);
           
            dialog.Content = panel;

            return dialog.ShowDialog() == true;
        }
    }
}
