using System;
using ReactiveTextBox;
using ReactiveTextBox.Lookup.Imperative;
using ReactiveTextBox.Lookup.Rx;

namespace ReactiveTextBoxTests.TestFactories.Imperative
{
    public class LookuperImperative2ProblemsExplainedTestFactory : ILookuperRxTestFactory
    {
        public ILookuperRx Create(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime) =>
            new LookuperImperativeAdapter(new LookuperImperative2ProblemsExplained(searchEngine, throttleDueTime, retryCount, timeoutDueTime));
    }
}