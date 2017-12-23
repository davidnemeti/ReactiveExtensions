using System;
using System.Reactive.Linq;

namespace ReactiveTextBox.Lookup.Rx
{
    public class LookuperRx03OnlyLatest : LookuperRxBase
    {
        public LookuperRx03OnlyLatest(ISearchEngine searchEngine) : base(searchEngine)
        {
        }

        protected override IObservable<string[]> Lookup(IObservable<string> texts) =>
            texts
                .__LOG_RAW_TEXT_FOR_DEMO()
                .__LOG_TEXT_FOR_DEMO()
                .Select(text =>
                    Observable.FromAsync(async ct => await SearchEngine.Search(text, ct))
                        .__LOG_TIMEINTERVAL_FOR_DEMO($"search: {text}")
                )
                .Switch();
    }
}
