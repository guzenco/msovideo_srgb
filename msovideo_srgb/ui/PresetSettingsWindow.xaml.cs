using System.Windows;
using System.Windows.Controls;

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
                DataGridSelectionHelper.ApplyCheckBoxStateToSelection(checkBox);
            }
        }

        public bool ChangedSettings => Preset.SettingsSourceMap.Changed;
    }
}