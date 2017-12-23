using System;
using System.Reactive.Linq;

namespace ReactiveTextBox.Lookup.Rx
{
    public class LookuperRx04WithThrottle : LookuperRxBase
    {
        private readonly TimeSpan _throttleDueTime;

        public LookuperRx04WithThrottle(ISearchEngine searchEngine)
            : this(searchEngine, throttleDueTime: TimeSpan.FromMilliseconds(500))
        {
        }

        public LookuperRx04WithThrottle(ISearchEngine searchEngine, TimeSpan throttleDueTime)
            : base(searchEngine)
        {
            _throttleDueTime = throttleDueTime;
        }

        protected override IObservable<string[]> Lookup(IObservable<string> texts) =>
            texts
                .__LOG_RAW_TEXT_FOR_DEMO()
                .Throttle(_throttleDueTime)
                .__LOG_TEXT_FOR_DEMO()
                .Select(text =>
                    Observable.FromAsync(async ct => await SearchEngine.Search(text, ct))
                        .__LOG_TIMEINTERVAL_FOR_DEMO($"search: {text}")
                )
                .Switch();
    }
}
