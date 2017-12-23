using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBox.Lookup.Imperative
{
    /// <summary>
    /// NOTE: This code is deliberately buggy.
    /// Here you can check the explanations for all of the problems.
    /// </summary>
    public class LookuperImperative2ProblemsExplained : LookuperImperativeBase
    {
        private readonly ISearchEngine _searchEngine;
        private readonly TimeSpan _throttleDueTime;
        private readonly int _retryCount;
        private readonly TimeSpan _timeoutDueTime;

        // NOTE: handling shared state needs extra care when dealing with concurrency (the state of this object represented by these fields is being shared between concurrent Lookup calls)
        // concurrency is not being handled properly in this example
        private DateTimeOffset? _previousTextTimestamp;
        private string _previousText;
        private CancellationTokenSource _ctsForCancel;  // PROBLEM: uninitialized cancellation token source
        private CancellationTokenSource _ctsForOngoingSearch;

        public LookuperImperative2ProblemsExplained(ISearchEngine searchEngine)
            : this(searchEngine, throttleDueTime: TimeSpan.FromMilliseconds(500), retryCount: 3, timeoutDueTime: TimeSpan.FromSeconds(3))
        {
        }

        public LookuperImperative2ProblemsExplained(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime)
        {
            _searchEngine = searchEngine;
            _throttleDueTime = throttleDueTime;
            _retryCount = retryCount;
            _timeoutDueTime = timeoutDueTime;
        }

        protected override async Task Lookup(string text)
        {
            var searchResult = await Lookup(text, _ctsForCancel.Token);

            if (searchResult != null)
            {
                SearchResult.SetItems(searchResult);
            }
        }

        protected override void Cancel()
        {
            _ctsForCancel.Cancel();
            // PROBLEM: CancellationTokenSources should be disposed!
            _ctsForCancel = new CancellationTokenSource();
        }

        private async Task<string[]> Lookup(string text, CancellationToken ct)
        {
            // PROBLEM: we do not await for the possibly ongoing task, thus introducing concurrency, which messes up the shared state

            LogExtensions.__LOG_RAW_TEXT_FOR_DEMO(text);

            // PROBLEM: completely wrong "rapid change detection" implementation (here we are not waiting before actually searching,
            // rather search immediately, and omit the last text if it is coming too soon)
            var currentTextTimestamp = DateTimeOffset.Now;
            if (currentTextTimestamp - _previousTextTimestamp < _throttleDueTime)
                return null;

            _previousTextTimestamp = currentTextTimestamp;

            if (_previousText == text)
                return null;

            // PROBLEM: we are setting _previousText before actually doing the search, therefore in case of the search being cancelled, an improperly set _previousText remains
            _previousText = text;

            LogExtensions.__LOG_TEXT_FOR_DEMO(text);

            // PROBLEM: we do not await for the cancelled task, thus introducing concurrency, which messes up the shared state
            // PROBLEM: _ctsForOngoingSearch might be null
            _ctsForOngoingSearch.Cancel();

            int trialIndex = 1;
            LSearch:
            // PROBLEM: CancellationTokenSources should be disposed!
            _ctsForOngoingSearch = new CancellationTokenSource();
            var ctsForTimeout = new CancellationTokenSource(_timeoutDueTime);
            var ctsMixed = CancellationTokenSource.CreateLinkedTokenSource(_ctsForOngoingSearch.Token, ctsForTimeout.Token, ct);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    LogExtensions.__LOG_FOR_DEMO($"BEGIN search: \"{text}\"");

                    var searchResult = await _searchEngine.Search(text, ctsMixed.Token);

                    stopwatch.Stop();
                    LogExtensions.__LOG_FOR_DEMO($"END search: \"{text}\" ({stopwatch.ElapsedMilliseconds} ms)");

                    return searchResult;
                }
                catch (OperationCanceledException)
                {
                    LogExtensions.__LOG_FOR_DEMO($"CANCEL search: \"{text}\" ({stopwatch.ElapsedMilliseconds} ms)");
                    throw;
                }
            }
            // PROBLEM: we forgot to handle the cancellation of ongoing search that has been cancelled by a subsequent search
            catch (OperationCanceledException) when (_ctsForCancel.IsCancellationRequested) // PROBLEM: we should check for ct instead of _ctsForCancel (ct belongs to this task, while _ctsForCancel is changing upon cancellation)
            {
                return new[] { "<< CANCEL >>" };
            }
            catch (Exception ex) when (trialIndex < _retryCount)
            {
                LogExtensions.__LOG_ERROR_FOR_DEMO("RETRY", ex);
                trialIndex++;
                goto LSearch;
            }
            catch (OperationCanceledException ex)   // this is the timeout exception
            {
                LogExtensions.__LOG_ERROR_FOR_DEMO("TIMEOUT", ex);
                return new[] { "<< TIMEOUT >>" };
            }
            catch (Exception ex)
            {
                LogExtensions.__LOG_ERROR_FOR_DEMO("ERROR", ex);
                return new[] { "<< ERROR >>" };
            }
        }
    }
}