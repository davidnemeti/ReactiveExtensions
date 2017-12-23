using System;
using System.Reactive.Linq;

namespace ReactiveTextBox.Lookup.Rx
{
    public class LookuperRx06WithRetryOnError : LookuperRxBase
    {
        private readonly TimeSpan _throttleDueTime;
        private readonly int _retryCount;

        public LookuperRx06WithRetryOnError(ISearchEngine searchEngine)
            : this(searchEngine, throttleDueTime: TimeSpan.FromMilliseconds(500), retryCount: 3)
        {
        }

        public LookuperRx06WithRetryOnError(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount)
            : base(searchEngine)
        {
            _throttleDueTime = throttleDueTime;
            _retryCount = retryCount;
        }

        protected override IObservable<string[]> Lookup(IObservable<string> texts) =>
            texts
                .__LOG_RAW_TEXT_FOR_DEMO()
                .Throttle(_throttleDueTime)
                .DistinctUntilChanged()
                .__LOG_TEXT_FOR_DEMO()
                .Select(text =>
                    Observable.FromAsync(async ct => await SearchEngine.Search(text, ct))
                        .__LOG_TIMEINTERVAL_FOR_DEMO($"search: {text}")
                        .__LOG_ERROR_FOR_DEMO("RETRY")
                        .Retry(_retryCount)
                        .Catch((Exception ex) => Observable.Return(new[] { "<< ERROR >>" }).__LOG_ERROR_FOR_DEMO("FATAL ERROR", ex))
                )
                .Switch();
    }
}
