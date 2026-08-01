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

        public static void NotifyDialog(string text, string title = null)
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
            Button okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 6, 0), IsDefault = true, IsCancel = true };

            panel.Children.Add(label);

            buttons.Children.Add(okButton);
            panel.Children.Add(buttons);

            dialog.Content = panel;

            dialog.ShowDialog();
        }

        public static Hotkey HotkeyDialog(Hotkey hotkey = null, string title = null)
        {
            if (hotkey == null)
            {
                hotkey = new Hotkey();
            }

            var keyModifiers = EnumExtensions.ToNamedArray<KeyModifier>();
            var virtualKeys = EnumExtensions.ToNamedArray<VirtualKey>();

            Window dialog = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            StackPanel inputs = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            ComboBox keyModifierCombobBox = new ComboBox { ItemsSource = keyModifiers, DisplayMemberPath = "Name", SelectedValuePath = "Value", VerticalAlignment = VerticalAlignment.Center };
            keyModifierCombobBox.SelectedValue = hotkey.KeyModifier;

            Label plusLabel = new Label { Content = " + " };
            ComboBox vitualKeyComboBox = new ComboBox { ItemsSource = virtualKeys, DisplayMemberPath = "Name", SelectedValuePath = "Value", VerticalAlignment = VerticalAlignment.Center };
            vitualKeyComboBox.SelectedValue = hotkey.VirtualKey;

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            Button okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 6, 0), IsDefault = true };
            Button resetButton = new Button { Content = "Reset", Width = 75, Margin = new Thickness(0, 0, 6, 0) };
            Button cancelButton = new Button { Content = "Cancel", Width = 75, Margin = new Thickness(0, 0, 0, 0), IsCancel = true };

            okButton.Click += (s, e) =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            };

            resetButton.Click += (s, e) =>
            {
                dialog.DialogResult = true;
                keyModifierCombobBox.SelectedValue = KeyModifier.None;
                vitualKeyComboBox.SelectedValue = VirtualKey.None;
                dialog.Close();
            };

            inputs.Children.Add(keyModifierCombobBox);
            inputs.Children.Add(plusLabel);
            inputs.Children.Add(vitualKeyComboBox);
            panel.Children.Add(inputs);

            buttons.Children.Add(okButton);
            buttons.Children.Add(resetButton);
            buttons.Children.Add(cancelButton);
            panel.Children.Add(buttons);

            dialog.Content = panel;

            if (dialog.ShowDialog() == true)
            {
                return new Hotkey((uint)keyModifierCombobBox.SelectedValue, (uint)vitualKeyComboBox.SelectedValue);
            }

            return null;
        }
    }
}
