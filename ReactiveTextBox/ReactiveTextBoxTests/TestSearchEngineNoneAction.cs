using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBoxTests
{
    public class TestSearchEngineNoneAction : ITestSearchEngineAction
    {
        public Task Execute(CancellationToken ct) => Task.CompletedTask;
    }
}
