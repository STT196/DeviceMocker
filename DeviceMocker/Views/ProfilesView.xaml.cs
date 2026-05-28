using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeviceMocker.Views
{
    public partial class ProfilesView : UserControl
    {
        private static readonly Regex DigitsRegex = new("^[0-9]+$");

        public ProfilesView()
        {
            InitializeComponent();
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !DigitsRegex.IsMatch(e.Text);
        }
    }
}
