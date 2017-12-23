using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBoxTests
{
    public class TestSearchEngineInfiniteAction : ITestSearchEngineAction
    {
        public async Task Execute(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<object>();
            using (ct.Register(() => tcs.SetCanceled()))
            {
                await tcs.Task;
            }
        }
    }
}
