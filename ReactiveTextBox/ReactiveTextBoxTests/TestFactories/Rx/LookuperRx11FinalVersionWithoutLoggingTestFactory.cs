using System;
using ReactiveTextBox;
using ReactiveTextBox.Lookup.Rx;

namespace ReactiveTextBoxTests.TestFactories.Rx
{
    public class LookuperRx11FinalVersionWithoutLoggingTestFactory : ILookuperRxTestFactory
    {
        public ILookuperRx Create(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime) =>
            new LookuperRx11FinalVersionWithoutLogging(searchEngine, throttleDueTime, retryCount, timeoutDueTime);
    }
}