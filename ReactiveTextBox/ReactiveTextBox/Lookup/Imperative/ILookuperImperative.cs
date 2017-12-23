using System.Threading.Tasks;

namespace ReactiveTextBox.Lookup.Imperative
{
    public interface ILookuperImperative
    {
        Task Lookup(string text);
        void Cancel();
        RangeObservableCollection<string> SearchResult { get; }
    }
}
