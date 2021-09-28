using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace BSU.GUI.UserControls
{
    public class ComboBoxPopup : Popup
    {
        // Thank you Stack Overflow <3 https://stackoverflow.com/a/5821819
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var isOpen = IsOpen;
            base.OnPreviewMouseLeftButtonDown(e);
            if (isOpen && !IsOpen)
                e.Handled = true;
        }
    }
}
