using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Controls;

namespace ReactiveTextBox.Lookup.Rx
{
    public class LookuperRx10WithCancellationByAmb : LookuperRxBase
    {
        private readonly TimeSpan _throttleDueTime;
        private readonly int _retryCount;
        private readonly TimeSpan _timeoutDueTime;

        private Action<bool> _setCancelButtonAccessibility;

        public LookuperRx10WithCancellationByAmb(ISearchEngine searchEngine)
            : this(searchEngine, throttleDueTime: TimeSpan.FromMilliseconds(500), retryCount: 3, timeoutDueTime: TimeSpan.FromSeconds(3))
        {
        }

        public LookuperRx10WithCancellationByAmb(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime)
            : base(searchEngine)
        {
            _throttleDueTime = throttleDueTime;
            _retryCount = retryCount;
            _timeoutDueTime = timeoutDueTime;
        }

        protected override IObservable<string[]> Lookup(IObservable<string> texts) =>
            texts
                .__LOG_RAW_TEXT_FOR_DEMO()
                .Throttle(_throttleDueTime)
                .DistinctUntilChanged()
                .__LOG_TEXT_FOR_DEMO()
                .__DO_FOR_CONTROL(text => SetCancelButtonAccessibility(enable: true))
                .Select(text =>
                    Observable.FromAsync(async ct => await SearchEngine.Search(text, ct))
                        .__LOG_TIMEINTERVAL_FOR_DEMO($"search: {text}")
                        .Timeout(_timeoutDueTime)
                        .__LOG_ERROR_FOR_DEMO("RETRY")
                        .Retry(_retryCount)
                        .Catch((TimeoutException ex) => Observable.Return(new[] {"<< TIMEOUT >>"}).__LOG_ERROR_FOR_DEMO("TIMEOUT", ex))
                        .Catch((Exception ex) => Observable.Return(new[] {"<< ERROR >>"}).__LOG_ERROR_FOR_DEMO("FATAL ERROR", ex))
                        .Amb(Cancellations.FirstAsync().Select(unit => new[] { "<< CANCEL >>" }).__LOG_FOR_DEMO("CANCEL"))
                )
                .Switch()
                .__DO_FOR_CONTROL(text => SetCancelButtonAccessibility(enable: false));

        public override void UseCancelButton(Button cancelButton)
        {
            base.UseCancelButton(cancelButton);

            cancelButton.IsEnabled = false;
            _setCancelButtonAccessibility = enabled => cancelButton.IsEnabled = enabled;
        }

        public override void Cancel()
        {
            Cancellations.OnNext(Unit.Default);
        }

        private void SetCancelButtonAccessibility(bool enable) => _setCancelButtonAccessibility?.Invoke(enable);
    }
}
