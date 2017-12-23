using System.Windows.Controls;

namespace ReactiveTextBox.Lookup
{
    public interface ILookuperWpf
    {
        void UseTextBox(TextBox textBox);
        void UseCancelButton(Button cancelButton);
        RangeObservableCollection<string> SearchResult { get; }
    }
}
