using System.Windows.Controls;
using System.Windows.Input;

namespace Ark.UI
{
    public class EasilyDefocusableTextBox : TextBox
    {
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }
}
