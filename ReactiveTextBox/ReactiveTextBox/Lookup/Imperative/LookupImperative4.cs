using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBox.Lookup.Imperative
{
    public class LookuperImperative4FinalWithoutLogging : LookuperImperativeBase
    {
        private readonly ISearchEngine _searchEngine;
        private readonly TimeSpan _throttleDueTime;
        private readonly int _retryCount;
        private readonly TimeSpan _timeoutDueTime;

        // NOTE: handling shared state needs extra care when dealing with concurrency (the state of this object represented by these fields is being shared between concurrent Lookup calls)
        private string _previousText = null;   // [2]
        private CancellationTokenSource _ctsForOngoingSearch = null;    // [8]
        private CancellationTokenSource _ctsForCancel = new CancellationTokenSource();   // [9]
        private Task<string[]> _ongoingSearch = null;    // [8]

        public LookuperImperative4FinalWithoutLogging(ISearchEngine searchEngine)
            : this(searchEngine, throttleDueTime: TimeSpan.FromMilliseconds(500), retryCount: 3, timeoutDueTime: TimeSpan.FromSeconds(3))
        {
        }

        public LookuperImperative4FinalWithoutLogging(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime)
        {
            _searchEngine = searchEngine;
            _throttleDueTime = throttleDueTime;
            _retryCount = retryCount;
            _timeoutDueTime = timeoutDueTime;
        }

        protected override async Task Lookup(string text)
        {
            var searchResult = await Lookup(text, _ctsForCancel.Token); // [9]

            if (searchResult != null)
            {
                SearchResult.SetItems(searchResult);
            }
        }

        protected override void Cancel()
        {
            _ctsForCancel.Cancel();     // [9]
            _ctsForCancel.Dispose();    // [9]
            _ctsForCancel = new CancellationTokenSource();  // [9]
        }

        private async Task<string[]> Lookup(string text, CancellationToken ct)   // [9]
        {
            _ctsForOngoingSearch?.Cancel(); // [1] [8]
            await Helpers.NullSafeAwait(_ongoingSearch);    // [1] [8] we need to await in order to avoid the execution of concurrent Lookup calls which would mess up the shared state
            _ongoingSearch = LookupInternal();
            return await _ongoingSearch;

            async Task<string[]> LookupInternal()
            {
                try
                {
                    using (_ctsForOngoingSearch = new CancellationTokenSource())    // [1] [8]
                    {
                        try
                        {
                            await Task.Delay(_throttleDueTime, _ctsForOngoingSearch.Token);    // [1]
                        }
                        catch (OperationCanceledException)
                        {
                            return null;    // [1] we have no better way to indicate that we do not want to produce an output (the caller needs to handle it specially)
                        }

                        if (_previousText == text)  // [2]
                            return null;    // [2] we have no better way to indicate that we do not want to produce an output (the caller needs to handle it specially)

                        int trialIndex = 1; // [5]
                        LSearch:    // [5]
                        using (var ctsForTimeout = new CancellationTokenSource(_timeoutDueTime))    // [4]
                        using (var ctsMixed = CancellationTokenSource.CreateLinkedTokenSource(_ctsForOngoingSearch.Token, ctsForTimeout.Token, ct)) // [4] [8] [9]
                        {
                            try
                            {
                                var searchResult = await _searchEngine.Search(text, ctsMixed.Token);    // [3] [4] [8]
                                _previousText = text;   // [2]
                                return searchResult;    // [3]
                            }
                            catch (OperationCanceledException) when (_ctsForOngoingSearch.IsCancellationRequested)  // [8] ongoing search has been cancelled by a subsequent search
                            {
                                return null;    // [8] we have no better way to indicate that we do not want to produce an output (the caller needs to handle it specially)
                            }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested)  // [9]
                            {
                                return new[] { "<< CANCEL >>" };    // [9]
                            }
                            catch (Exception) when (trialIndex < _retryCount)  // [5]
                            {
                                trialIndex++;   // [5]
                                goto LSearch;   // [5]
                            }
                            catch (OperationCanceledException)   // [6] this is the timeout exception
                            {
                                return new[] { "<< TIMEOUT >>" };   // [6]
                            }
                            catch (Exception)    // [7]
                            {
                                return new[] { "<< ERROR >>" };     // [7]
                            }
                        }
                    }
                }
                finally
                {
                    _ctsForOngoingSearch = null;    // [1] [8]
                    _ongoingSearch = null;          // [1] [8]
                }
            }
        }
    }
}
