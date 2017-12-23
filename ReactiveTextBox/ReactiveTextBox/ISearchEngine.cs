using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBox
{
    public interface ISearchEngine
    {
        Task<string[]> Search(string text, CancellationToken ct);
    }
}
