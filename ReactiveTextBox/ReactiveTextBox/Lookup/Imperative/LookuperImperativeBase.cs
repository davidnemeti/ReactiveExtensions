using System.Threading.Tasks;
using System.Windows.Controls;

namespace ReactiveTextBox.Lookup.Imperative
{
    public abstract class LookuperImperativeBase : ILookuperWpf, ILookuperImperative
    {
        #region ILookuperWpf

        public void UseTextBox(TextBox textBox)
        {
            // NOTE: we should unsubscribe from the previously used textBox.TextChanged, if any (not implemented here)
            textBox.TextChanged += async (sender, args) =>
            {
                await Lookup(textBox.Text);
            };
        }

        public void UseCancelButton(Button cancelButton)
        {
            cancelButton.IsEnabled = true;

            // NOTE: we should unsubscribe from the previously used cancelButton.Click, if any (not implemented here)
            cancelButton.Click += (sender, args) =>
            {
                Cancel();
            };
        }

        public RangeObservableCollection<string> SearchResult { get; } = new RangeObservableCollection<string>();

        #endregion

        #region ILookuperImperative

        Task ILookuperImperative.Lookup(string text) => Lookup(text);
        void ILookuperImperative.Cancel() => Cancel();
        // SearchResult is implemented as public since it is a member of ILookuperWpf as well

        #endregion

        #region Abstract methods

        protected abstract Task Lookup(string text);
        protected abstract void Cancel();

        #endregion
    }
}
