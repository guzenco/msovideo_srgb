using System.Windows.Controls;
using System.Windows;

namespace msovideo_srgb
{
    public class ComboBoxTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DropdownTemplate { get; set; }
        public DataTemplate SelectionTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is ContentPresenter cp)
            {
                if (cp.TemplatedParent is ComboBoxItem)
                {
                    return DropdownTemplate;
                }

                if (cp.TemplatedParent is ComboBox)
                {
                    return SelectionTemplate;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}