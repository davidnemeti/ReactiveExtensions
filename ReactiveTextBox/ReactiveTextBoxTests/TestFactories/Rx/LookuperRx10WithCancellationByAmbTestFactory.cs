using System;
using ReactiveTextBox;
using ReactiveTextBox.Lookup.Rx;

namespace ReactiveTextBoxTests.TestFactories.Rx
{
    public class LookuperRx10WithCancellationByAmbTestFactory : ILookuperRxTestFactory
    {
        public ILookuperRx Create(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime) =>
            new LookuperRx10WithCancellationByAmb(searchEngine, throttleDueTime, retryCount, timeoutDueTime);
    }
}