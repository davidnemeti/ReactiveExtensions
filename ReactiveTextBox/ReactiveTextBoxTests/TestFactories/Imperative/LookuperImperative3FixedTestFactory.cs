using System;
using ReactiveTextBox;
using ReactiveTextBox.Lookup.Imperative;
using ReactiveTextBox.Lookup.Rx;

namespace ReactiveTextBoxTests.TestFactories.Imperative
{
    public class LookuperImperative3FixedTestFactory : ILookuperRxTestFactory
    {
        public ILookuperRx Create(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime) =>
            new LookuperImperativeAdapter(new LookuperImperative3Fixed(searchEngine, throttleDueTime, retryCount, timeoutDueTime));
    }
}