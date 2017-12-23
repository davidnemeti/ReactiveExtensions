using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBox.Lookup.Imperative
{
    /// <summary>
    /// NOTE: This code is deliberately buggy.
    /// You can try and find all the bugs, or can check the explanations for all of the problems in <see cref="LookuperImperative2ProblemsExplained"/>.
    /// </summary>
    public class LookuperImperative1Problems : LookuperImperativeBase
    {
        private readonly ISearchEngine _searchEngine;
        private readonly TimeSpan _throttleDueTime;
        private readonly int _retryCount;
        private readonly TimeSpan _timeoutDueTime;

        private DateTimeOffset? _previousTextTimestamp;
        private string _previousText;
        private CancellationTokenSource _ctsForCancel;
        private CancellationTokenSource _ctsForOngoingSearch;

        public LookuperImperative1Problems(ISearchEngine searchEngine)
            : this(searchEngine, throttleDueTime: TimeSpan.FromMilliseconds(500), retryCount: 3, timeoutDueTime: TimeSpan.FromSeconds(3))
        {
        }

        public LookuperImperative1Problems(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime)
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
            _ctsForCancel = new CancellationTokenSource();
        }

        private async Task<string[]> Lookup(string text, CancellationToken ct)
        {
            LogExtensions.__LOG_RAW_TEXT_FOR_DEMO(text);

            var currentTextTimestamp = DateTimeOffset.Now;
            if (currentTextTimestamp - _previousTextTimestamp < _throttleDueTime)
                return null;

            _previousTextTimestamp = currentTextTimestamp;

            if (_previousText == text)
                return null;

            _previousText = text;

            LogExtensions.__LOG_TEXT_FOR_DEMO(text);

            _ctsForOngoingSearch?.Cancel();

            int trialIndex = 1;
            LSearch:
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
            catch (OperationCanceledException) when (_ctsForCancel.IsCancellationRequested)
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
