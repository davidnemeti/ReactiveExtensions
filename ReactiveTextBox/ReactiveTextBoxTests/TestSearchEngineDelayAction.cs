using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBoxTests
{
    public class TestSearchEngineDelayAction : ITestSearchEngineAction
    {
        private readonly TimeSpan _timeSpan;

        public TestSearchEngineDelayAction(TimeSpan timeSpan)
        {
            _timeSpan = timeSpan;
        }

        public async Task Execute(CancellationToken ct)
        {
            await Task.Delay(_timeSpan, ct);
        }
    }
}
