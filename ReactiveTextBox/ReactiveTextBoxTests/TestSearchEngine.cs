using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ReactiveTextBox;

namespace ReactiveTextBoxTests
{
    public class TestSearchEngine : ISearchEngine
    {
        private readonly IReadOnlyList<string> _names;
        private IEnumerable<ITestSearchEngineAction> _testSearchEngineActions = Enumerable.Empty<ITestSearchEngineAction>();

        private readonly ReplaySubject<string> _searchBeginnings = new ReplaySubject<string>();
        private readonly ReplaySubject<(string, string[])> _searchEndings = new ReplaySubject<(string, string[])>();

        private int _searchCount = 0;

        public TestSearchEngine(params string[] names)
        {
            _names = names.ToList();
        }

        public IObservable<string> SearchBeginnings => _searchBeginnings;
        public IObservable<(string, string[])> SearchEndings => _searchEndings;

        public void SetActionsWithRepeatingTheLastOneForever(params ITestSearchEngineAction[] testSearchEngineActions)
        {
            SetActions(
                testSearchEngineActions
                    .AsEnumerable()
                    .Concat(EnumerableEx.Repeat(testSearchEngineActions.Last()))
            );
        }

        public void SetActions(params ITestSearchEngineAction[] testSearchEngineActions)
        {
            SetActions(testSearchEngineActions.AsEnumerable());
        }

        public void SetActions(IEnumerable<ITestSearchEngineAction> testSearchEngineActions)
        {
            _testSearchEngineActions = testSearchEngineActions.Memoize();
        }

        public async Task<string[]> Search(string text, CancellationToken ct)
        {
            int searchCountOriginal = _searchCount;
            _searchCount++;

            _searchBeginnings.OnNext(text);

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var testSearchEngineAction = _testSearchEngineActions.ElementAtOrDefault(searchCountOriginal);
            if (testSearchEngineAction != null)
            {
                await testSearchEngineAction.Execute(ct);
            }

            var searchResult =
                (
                    from name in _names
                    where name.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1
                    orderby name
                    select name
                )
                .ToArray();

            _searchEndings.OnNext((text, searchResult));

            return searchResult;
        }
    }
}
