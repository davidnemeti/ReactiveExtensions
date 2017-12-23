using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBoxTests
{
    public class TestSearchEngineThrowExceptionAction : ITestSearchEngineAction
    {
        private readonly Exception _exception;

        public TestSearchEngineThrowExceptionAction()
            : this(new ApplicationException("Test search engine exception"))
        {
        }

        public TestSearchEngineThrowExceptionAction(Exception exception)
        {
            _exception = exception;
        }

        public Task Execute(CancellationToken ct)
        {
            throw _exception;
        }
    }
}
