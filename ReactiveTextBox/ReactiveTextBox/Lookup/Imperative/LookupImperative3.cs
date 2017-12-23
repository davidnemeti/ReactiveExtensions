using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBox.Lookup.Imperative
{
    public class LookuperImperative3Fixed : LookuperImperativeBase
    {
        private readonly ISearchEngine _searchEngine;
        private readonly TimeSpan _throttleDueTime;
        private readonly int _retryCount;
        private readonly TimeSpan _timeoutDueTime;

        private string _previousText = null;
        private CancellationTokenSource _ctsForOngoingSearch = null;
        private CancellationTokenSource _ctsForCancel = new CancellationTokenSource();
        private Task<string[]> _ongoingSearch = null;

        public LookuperImperative3Fixed(ISearchEngine searchEngine)
            : this(searchEngine, throttleDueTime: TimeSpan.FromMilliseconds(500), retryCount: 3, timeoutDueTime: TimeSpan.FromSeconds(3))
        {
        }

        public LookuperImperative3Fixed(ISearchEngine searchEngine, TimeSpan throttleDueTime, int retryCount, TimeSpan timeoutDueTime)
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
            _ctsForCancel.Dispose();
            _ctsForCancel = new CancellationTokenSource();
        }

        private async Task<string[]> Lookup(string text, CancellationToken ct)
        {
            _ctsForOngoingSearch?.Cancel();
            await SafeAwait(_ongoingSearch);
            _ongoingSearch = LookupInternal();
            return await _ongoingSearch;

            async Task<string[]> LookupInternal()
            {
                LogExtensions.__LOG_RAW_TEXT_FOR_DEMO(text);

                try
                {
                    using (_ctsForOngoingSearch = new CancellationTokenSource())
                    {
                        try
                        {
                            await Task.Delay(_throttleDueTime, _ctsForOngoingSearch.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            return null;
                        }

                        if (_previousText == text)
                            return null;

                        LogExtensions.__LOG_TEXT_FOR_DEMO(text);

                        int trialIndex = 1;
                        LSearch:
                        using (var ctsForTimeout = new CancellationTokenSource(_timeoutDueTime))
                        using (var ctsMixed = CancellationTokenSource.CreateLinkedTokenSource(_ctsForOngoingSearch.Token, ctsForTimeout.Token, ct))
                        {
                            try
                            {
                                var stopwatch = Stopwatch.StartNew();
                                try
                                {
                                    LogExtensions.__LOG_FOR_DEMO($"BEGIN search: \"{text}\"");

                                    var searchResult = await _searchEngine.Search(text, ctsMixed.Token);
                                    _previousText = text;

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
                            catch (OperationCanceledException) when (_ctsForOngoingSearch.IsCancellationRequested)  // ongoing search has been cancelled by a subsequent search
                            {
                                return null;
                            }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested)
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
                finally
                {
                    _ctsForOngoingSearch = null;
                    _ongoingSearch = null;
                }
            }
        }

        private static async Task SafeAwait(Task task)
        {
            try
            {
                await task;
            }
            catch (NullReferenceException)
            {
                // NOTE: catching a NullReferenceException seems to be horrible hack;
                // however the other solution would be to lock for the given task when getting/setting its value, which just would make the code even more complex
            }
        }
    }
}