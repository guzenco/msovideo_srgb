using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace msovideo_srgb
{
    public static class DataGridSelectionHelper
    {
        public static void ApplyCheckBoxStateToSelection(CheckBox checkBox)
        {
            var dataGrid = FindParent<DataGrid>(checkBox);
            if (dataGrid == null || dataGrid.SelectedItems.Count <= 1) return;

            var binding = BindingOperations.GetBinding(checkBox, CheckBox.IsCheckedProperty);
            string fieldName = binding?.Path?.Path;

            if (fieldName == null) return;

            foreach (var item in dataGrid.SelectedItems)
            {
                var prop = item.GetType().GetProperty(fieldName);
                if(prop == null) continue;

                var val = prop.GetValue(item);
                if (val?.Equals(checkBox.IsChecked) == true) continue;

                prop.SetValue(item, checkBox.IsChecked);
            }
        }

        public static void EnsureSingleSelection(FrameworkElement element)
        {
            var dataGrid = FindParent<DataGrid>(element);
            if (dataGrid == null) return;

            var row = FindParent<DataGridRow>(element);
            if (row == null) return;

            if(dataGrid.SelectedItems.Count == 1 && dataGrid.SelectedItems.Contains(row.Item)) return;

            dataGrid.SelectedItems.Clear();
            dataGrid.SelectedItem = row.Item;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }
    }
}
