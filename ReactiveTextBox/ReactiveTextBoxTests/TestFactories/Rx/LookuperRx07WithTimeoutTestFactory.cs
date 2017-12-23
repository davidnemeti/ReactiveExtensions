using System;
using ReactiveTextBox;
using ReactiveTextBox.Lookup.Rx;

namespace ReactiveTextBoxTests.TestFactories.Rx
{
    public class LookuperRx07WithTimeoutTestFactory : ILookuperRxTestFactory
    {
        public ILookuperRx Create(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime) =>
            new LookuperRx07WithTimeout(searchEngine, throttleDueTime, retryCount, timeoutDueTime);
    }
}