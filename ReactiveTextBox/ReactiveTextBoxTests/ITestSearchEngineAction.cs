using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBoxTests
{
    public interface ITestSearchEngineAction
    {
        Task Execute(CancellationToken ct);
    }
}
