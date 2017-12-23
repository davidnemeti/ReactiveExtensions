using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Controls;

namespace ReactiveTextBox.Lookup.Rx
{
    public abstract class LookuperRxBase : ILookuperWpf, ILookuperRx
    {
        private IDisposable _searchResultsSubscription;
        private IDisposable _cancellationSubscription;

        protected ISearchEngine SearchEngine { get; }
        protected Subject<Unit> Cancellations { get; } = new Subject<Unit>();

        protected LookuperRxBase(ISearchEngine searchEngine)
        {
            SearchEngine = searchEngine;
        }

        #region ILookuperWpf

        public void UseTextBox(TextBox textBox)
        {
            var texts = Observable.FromEventPattern(textBox, nameof(TextBox.TextChanged)).Select(_ => textBox.Text);
            SubscribeToTextsForSearchResult(texts);
        }

        public virtual void UseCancelButton(Button cancelButton)
        {
            cancelButton.IsEnabled = true;

            var cancellations = Observable.FromEventPattern(cancelButton, nameof(Button.Click)).Select(_ => Unit.Default);
            SubscribeToCancellations(cancellations);
        }

        public RangeObservableCollection<string> SearchResult { get; } = new RangeObservableCollection<string>();

        #endregion

        #region ILookuperRx

        IObservable<string[]> ILookuperRx.Lookup(IObservable<string> texts) => Lookup(texts);

        public virtual void Cancel()
        {
            // not implemented here - derived classes can implement it
        }

        #endregion

        #region Abstract methods

        protected abstract IObservable<string[]> Lookup(IObservable<string> texts);

        #endregion

        #region Helpers

        protected void SubscribeToTextsForSearchResult(IObservable<string> texts)
        {
            _searchResultsSubscription?.Dispose();   // this unsubscribe from the previously used subscription, if any

            var searchResults = Lookup(texts);

            _searchResultsSubscription = searchResults
                .ObserveOnIfContextIsNotNull(SynchronizationContext.Current)
                .Subscribe(onNext: searchResult =>
                {
                    SearchResult.SetItems(searchResult);
                });
        }

        private void SubscribeToCancellations(IObservable<Unit> cancellations)
        {
            _cancellationSubscription?.Dispose();   // this unsubscribe from the previously used subscription, if any

            _cancellationSubscription = cancellations.Subscribe(onNext: cancellation => Cancel());
        }

        #endregion
    }
}
