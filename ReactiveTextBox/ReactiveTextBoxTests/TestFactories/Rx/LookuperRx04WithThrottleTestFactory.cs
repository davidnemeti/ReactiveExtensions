using System;
using ReactiveTextBox;
using ReactiveTextBox.Lookup.Rx;

namespace ReactiveTextBoxTests.TestFactories.Rx
{
    public class LookuperRx04WithThrottleTestFactory : ILookuperRxTestFactory
    {
        public ILookuperRx Create(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime) =>
            new LookuperRx04WithThrottle(searchEngine, throttleDueTime);
    }
}