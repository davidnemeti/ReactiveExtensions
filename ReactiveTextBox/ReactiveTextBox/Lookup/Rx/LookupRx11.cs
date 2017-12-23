using System;
using System.Reactive;
using System.Reactive.Linq;

namespace ReactiveTextBox.Lookup.Rx
{
    /*
     * 1. Wait before search in case of text changing rapidly: Throttle
     * 2. Search only if text has been actually changed: DistinctUntilChanged
     * 3. Search asynchronously returning with proper search result: Observable.FromAsync + Search
     * 4. Set timeout for the search: Timeout
     * 5. Retry search in case of any error: Retry
     * 6. Catch TimeoutException during search and return "<< TIMEOUT >>": Catch(TimeoutException ex)
     * 7. Catch any other exceptions during search and return "<< ERROR >>": Catch(Exception ex)
     * 8. Search tasks should be executed sequentially (so an obsolete, slow search should not overwrite current search result),
     *    and we should switch to new search task in case of receiving new text, and cancel ongoing search task: Switch
     * 9. The search should be cancellable: an IObservable is automatically cancellable, we just need to unsubscribe from it (call Dispose on the subscription);
     *    or we can use TakeUntil or Amb, if we want to weave the cancellation logic into the returned observable
     */
    public class LookuperRx11FinalVersionWithoutLogging : LookuperRxBase
    {
        private readonly TimeSpan _throttleDueTime;
        private readonly int _retryCount;
        private readonly TimeSpan _timeoutDueTime;

        public LookuperRx11FinalVersionWithoutLogging(ISearchEngine searchEngine)
            : this(searchEngine, throttleDueTime: TimeSpan.FromMilliseconds(500), retryCount: 3, timeoutDueTime: TimeSpan.FromSeconds(3))
        {
        }

        public LookuperRx11FinalVersionWithoutLogging(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime)
            : base(searchEngine)
        {
            _throttleDueTime = throttleDueTime;
            _retryCount = retryCount;
            _timeoutDueTime = timeoutDueTime;
        }

        protected override IObservable<string[]> Lookup(IObservable<string> texts) =>
            texts
                .Throttle(_throttleDueTime)   // [1]
                .DistinctUntilChanged() // [2]
                .Select(text =>
                        Observable.FromAsync(async ct => await SearchEngine.Search(text, ct))    // [3]
                            .Timeout(_timeoutDueTime)   // [4]
                            .Retry(_retryCount)   // [5]
                            .Catch((TimeoutException ex) => Observable.Return(new[] { "<< TIMEOUT >>" }))   // [6]
                            .Catch((Exception ex) => Observable.Return(new[] { "<< ERROR >>" }))    // [7]
                            .Amb(Cancellations.FirstAsync().Select(unit => new[] { "<< CANCEL >>" }))   // [9]
                )
                .Switch();  // [8]

        public override void Cancel()
        {
            Cancellations.OnNext(Unit.Default); // [9]
        }
    }
}
