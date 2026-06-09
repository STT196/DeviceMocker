using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace DeviceMocker.Views
{
    public partial class EmulatorsView : UserControl
    {
        private static readonly Regex DigitsRegex = new("^[0-9]+$");
        private ScrollViewer? _hostScrollViewer;
        private ScrollBarVisibility _hostVerticalScrollBarVisibility;
        private ScrollBarVisibility _hostHorizontalScrollBarVisibility;

        public EmulatorsView()
        {
            InitializeComponent();
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !DigitsRegex.IsMatch(e.Text);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _hostScrollViewer ??= FindAncestorScrollViewer(this);
            if (_hostScrollViewer == null)
            {
                return;
            }

            _hostVerticalScrollBarVisibility = _hostScrollViewer.VerticalScrollBarVisibility;
            _hostHorizontalScrollBarVisibility = _hostScrollViewer.HorizontalScrollBarVisibility;
            _hostScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            _hostScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_hostScrollViewer == null)
            {
                return;
            }

            _hostScrollViewer.VerticalScrollBarVisibility = _hostVerticalScrollBarVisibility;
            _hostScrollViewer.HorizontalScrollBarVisibility = _hostHorizontalScrollBarVisibility;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
            {
                return;
            }

            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void ReceiptPreview_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            var scrollViewer = FindDescendantScrollViewer(textBox);
            if (scrollViewer == null)
            {
                return;
            }

            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private static ScrollViewer? FindAncestorScrollViewer(DependencyObject dependencyObject)
        {
            var current = VisualTreeHelper.GetParent(dependencyObject);
            while (current != null)
            {
                if (current is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static ScrollViewer? FindDescendantScrollViewer(DependencyObject dependencyObject)
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(dependencyObject); index++)
            {
                var child = VisualTreeHelper.GetChild(dependencyObject, index);
                if (child is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }

                var nested = FindDescendantScrollViewer(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
