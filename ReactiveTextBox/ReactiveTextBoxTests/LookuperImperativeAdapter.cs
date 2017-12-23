using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveTextBox.Lookup.Imperative;
using ReactiveTextBox.Lookup.Rx;

namespace ReactiveTextBoxTests
{
    public class LookuperImperativeAdapter : ILookuperRx
    {
        private readonly ILookuperImperative _lookuperImperative;

        public LookuperImperativeAdapter(ILookuperImperative lookuperImperative)
        {
            _lookuperImperative = lookuperImperative;
        }

        public IObservable<string[]> Lookup(IObservable<string> texts)
        {
            var textsCompletedWithFinishedLookups = new Subject<Unit>();

            return Observable.Using(
                resourceFactory: () =>
                    texts
                        .SelectMany(text => Observable.FromAsync(async () => await _lookuperImperative.Lookup(text)))
                        .Subscribe(
                            onNext: _ => { },
                            onCompleted: () => textsCompletedWithFinishedLookups.OnNext(Unit.Default)
                        ),
                observableFactory: subscription =>
                    Observable.FromEventPattern(_lookuperImperative.SearchResult, nameof(_lookuperImperative.SearchResult.CollectionChanged))
                        .Select(_ => _lookuperImperative.SearchResult.ToArray())
                        .TakeUntil(textsCompletedWithFinishedLookups)
            );
        }

        public void Cancel()
        {
            _lookuperImperative.Cancel();
        }
    }
}
