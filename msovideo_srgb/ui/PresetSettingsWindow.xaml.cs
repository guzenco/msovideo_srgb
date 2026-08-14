using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace msovideo_srgb
{
    public partial class PresetSettingsWindow
    {
        public Preset Preset { get; set; }

        public PresetSettingsWindow(Preset preset)
        {
            Preset = preset;
            DataContext = this;
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var binding = BindingOperations.GetBinding(checkBox, CheckBox.IsCheckedProperty);
                string fieldName = binding?.Path?.Path;

                if (fieldName == null) return;

                foreach (var item in SettingsSourceMapGrid.SelectedItems)
                {
                    var prop = item.GetType().GetProperty(fieldName);
                    prop?.SetValue(item, checkBox.IsChecked);
                }
            }
        }

        public bool ChangedSettings => Preset.SettingsSourceMap.Changed;
    }
}