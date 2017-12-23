using System;
using System.Reactive.Linq;

namespace ReactiveTextBox.Lookup.Rx
{
    public class LookuperRx08WithCancellationByResubscribe : LookuperRxBase
    {
        private readonly TimeSpan _throttleDueTime;
        private readonly int _retryCount;
        private readonly TimeSpan _timeoutDueTime;

        private IObservable<string> _texts;

        public LookuperRx08WithCancellationByResubscribe(ISearchEngine searchEngine)
            : this(searchEngine, throttleDueTime: TimeSpan.FromMilliseconds(500), retryCount: 3, timeoutDueTime: TimeSpan.FromSeconds(3))
        {
        }

        public LookuperRx08WithCancellationByResubscribe(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime)
            : base(searchEngine)
        {
            _throttleDueTime = throttleDueTime;
            _retryCount = retryCount;
            _timeoutDueTime = timeoutDueTime;
        }

        protected override IObservable<string[]> Lookup(IObservable<string> texts)
        {
            _texts = texts;

            return texts
                .__LOG_RAW_TEXT_FOR_DEMO()
                .Throttle(_throttleDueTime)
                .DistinctUntilChanged()
                .__LOG_TEXT_FOR_DEMO()
                .Select(text =>
                    Observable.FromAsync(async ct => await SearchEngine.Search(text, ct))
                        .__LOG_TIMEINTERVAL_FOR_DEMO($"search: {text}")
                        .Timeout(_timeoutDueTime)
                        .__LOG_ERROR_FOR_DEMO("RETRY")
                        .Retry(_retryCount)
                        .Catch((TimeoutException ex) => Observable.Return(new[] {"<< TIMEOUT >>"}).__LOG_ERROR_FOR_DEMO("TIMEOUT", ex))
                        .Catch((Exception ex) => Observable.Return(new[] {"<< ERROR >>"}).__LOG_ERROR_FOR_DEMO("FATAL ERROR", ex))
                )
                .Switch();
        }

        public override void Cancel()
        {
            SubscribeToTextsForSearchResult(_texts);
        }
    }
}
