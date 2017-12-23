using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;
using ReactiveTextBox;
using ReactiveTextBox.Lookup.Rx;

// NOTE that although LookuperRx08WithCancellationByResubscribe supports cancellation, it cannot past the cancellation tests,
// because it handles cancellation by unsubscribing, and this cannot be tested easily with this Rx-like test implementation
// (we have other implementations that support cancellation and can pass the cancellation tests).

using LookuperTestFactory = ReactiveTextBoxTests.TestFactories.Rx.LookuperRx11FinalVersionWithoutLoggingTestFactory;  // change this to other lookuper test factories and run the tests to experiment
//using LookuperTestFactory = ReactiveTextBoxTests.TestFactories.Imperative.LookuperImperative4FinalWithoutLoggingTestFactory;  // we can test the imperative logic as well

namespace ReactiveTextBoxTests
{
    [TestFixture]
    public class Tests
    {
        private readonly ILookuperRxTestFactory _lookuperRxTestFactory = new LookuperTestFactory();

        /// <summary>
        /// NOTE that it should give the test cases enough time to run.
        /// See the default parameters in <see cref="LookuperWrapper"/>.
        /// </summary>
        private const int TEST_RUNNING_TIMEOUT_IN_MILLISECONDS = 3000;

        private static readonly TimeSpan DEFAULT_THROTTLE_DUE_TIME = TimeSpan.FromMilliseconds(100);
        private const int DEFAULT_RETRY_COUNT = 2;    // NOTE: it is actually the count of all tries (it actually will be one less retry)
        private static readonly TimeSpan DEFAULT_TIMEOUT_DUE_TIME = TimeSpan.FromMilliseconds(100);

        protected ILookuperRx GetLookuper(ISearchEngine searchEngine) => GetLookuper(searchEngine, DEFAULT_TIMEOUT_DUE_TIME);

        protected ILookuperRx GetLookuper(ISearchEngine searchEngine, TimeSpan timeoutDueTime) =>
            _lookuperRxTestFactory.Create(searchEngine, DEFAULT_THROTTLE_DUE_TIME, DEFAULT_RETRY_COUNT, timeoutDueTime);

        /// <summary>
        /// [3]: Observable.FromAsync + Search
        /// </summary>
        [Test]
        public async Task EmptyTextShouldResultEmptySearchList()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            await lookuper
                .Lookup(TextsForKeyPresses(string.Empty))
                .ShouldFinishWithSearchResult(Array.Empty<string>());
        }

        /// <summary>
        /// [3]: Observable.FromAsync + Search
        /// </summary>
        [Test]
        public async Task OneSpecificItemShouldBeFound()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            await lookuper
                .Lookup(TextsForKeyPresses("John"))
                .ShouldFinishWithSearchResult("John");
        }

        /// <summary>
        /// [3]: Observable.FromAsync + Search
        /// </summary>
        [Test]
        public async Task TwoSpecificItemsShouldBeFound()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            await lookuper
                .Lookup(TextsForKeyPresses("Jo"))
                .ShouldFinishWithSearchResult("John", "Joe");
        }

        /// <summary>
        /// [3]: Observable.FromAsync + Search
        /// </summary>
        [Test]
        public async Task TwoKeypressesShouldWork()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            await lookuper
                .Lookup(TextsForKeyPresses("Jo", "Joh"))
                .ShouldFinishWithSearchResult("John");
        }

        /// <summary>
        /// [8]: Switch
        /// </summary>
        /// <remarks>
        /// NOTE that for LookuperRx02Sequential this test would hang (would never end), hence the <see cref="TimeoutAttribute"/>.
        /// </remarks>
        [Test]
        [Timeout(TEST_RUNNING_TIMEOUT_IN_MILLISECONDS)]
        public async Task ObsoleteSlowSearchShouldNotOverwriteCurrentSearchResultManual()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            TestSearchEngineManualAction search1, search2;
            var lookuper = GetLookuper(searchEngine);

            searchEngine.SetActions(
                search1 = new TestSearchEngineManualAction(),
                search2 = new TestSearchEngineManualAction()
            );

            var keyPresses = TextsForKeyPressesWithWaitingForEachSearchBeginning(searchEngine.SearchBeginnings, "Jo", "Joh");

            var searchResultAssertTask = lookuper
                .Lookup(keyPresses)
                .ShouldFinishWithSearchResult("John");

            await search2.Complete();
            await search1.Complete();
            await searchResultAssertTask;
        }

        /// <summary>
        /// [8]: Switch
        /// </summary>
        /// <remarks>
        /// NOTE that for LookuperRx02Sequential this test would hang (would never end), hence the <see cref="TimeoutAttribute"/>.
        /// </remarks>
        [Test]
        [Timeout(TEST_RUNNING_TIMEOUT_IN_MILLISECONDS)]
        public async Task ObsoleteSlowSearchShouldNotOverwriteCurrentSearchResultDelayed()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            searchEngine.SetActions(
                new TestSearchEngineDelayAction(TimeSpan.FromMilliseconds(800)),
                new TestSearchEngineDelayAction(TimeSpan.FromMilliseconds(100))
            );

            var keyPresses = TextsForKeyPressesWithWaitingForEachSearchBeginning(searchEngine.SearchBeginnings, "Jo", "Joh");

            await lookuper
                .Lookup(keyPresses)
                .ShouldFinishWithSearchResult("John");
        }

        /// <summary>
        /// [1]: Throttle
        /// </summary>
        [Test]
        public async Task IgnoreRapidKeyPresses()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            var textsForKeyPressesAfter = new Subject<string>();

            var searchResults = lookuper
                .Lookup(TextsForKeyPresses("J", "Jo").Concat(textsForKeyPressesAfter))
                .Replay()
                .RefCount();

            await searchResults.FirstAsync();   // wait for ["J", "Jo"] to be processed

            textsForKeyPressesAfter.OnNext("Joh");
            textsForKeyPressesAfter.OnCompleted();

            await searchResults.ShouldHaveSearchResults(new[] {"John", "Joe" }, new[] { "John" });
        }

        /// <summary>
        /// [2]: DistinctUntilChanged
        /// </summary>
        [Test]
        public async Task IgnoreSameConsecutiveTexts()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            var textsForKeyPressesAfter = new Subject<string>();

            var searchResults = lookuper
                .Lookup(TextsForKeyPresses("Jo").Concat(textsForKeyPressesAfter))
                .Replay()
                .RefCount();

            await searchResults.FirstAsync();   // wait for ["Jo"] to be processed

            textsForKeyPressesAfter.OnNext("Jo");
            textsForKeyPressesAfter.OnCompleted();

            await searchResults.ShouldHaveSearchResults(new[] {"John", "Joe" });
        }

        /// <summary>
        /// [4]: Timeout
        /// </summary>
        /// <remarks>
        /// NOTE that for implementations below LookuperRx07WithTimeout this test would hang (would never end), hence the <see cref="TimeoutAttribute"/>.
        /// </remarks>
        [Test]
        [Timeout(TEST_RUNNING_TIMEOUT_IN_MILLISECONDS)]
        public async Task SearchShouldNotBeWaitedForeverButTimeOut()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            searchEngine.SetActions(new TestSearchEngineInfiniteAction());

            var searchResults = lookuper.Lookup(TextsForKeyPresses("Jo"));

            await searchResults.ShouldHaveSearchResults(new[] {"John", "Joe" });
        }

        /// <summary>
        /// [5]: Retry
        /// </summary>
        [Test]
        public async Task FailedSearchShouldBeRetried()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            searchEngine.SetActions(new TestSearchEngineThrowExceptionAction());

            var searchResults = lookuper.Lookup(TextsForKeyPresses("Jo"));

            await searchResults.ShouldHaveSearchResults(new[] {"John", "Joe" });
        }

        /// <summary>
        /// [6]: Catch(TimeoutException ex)
        /// </summary>
        /// <remarks>
        /// NOTE that for implementations below LookuperRx07WithTimeout this test would hang (would never end), hence the <see cref="TimeoutAttribute"/>.
        /// </remarks>
        [Test]
        [Timeout(TEST_RUNNING_TIMEOUT_IN_MILLISECONDS)]
        public async Task CatchTimeoutExceptionAndReturnProperResult()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            searchEngine.SetActionsWithRepeatingTheLastOneForever(new TestSearchEngineInfiniteAction());

            var searchResults = lookuper.Lookup(TextsForKeyPresses("Jo"));

            await searchResults.ShouldHaveSearchResults(new[] { "<< TIMEOUT >>" });
        }

        /// <summary>
        /// [7]: Catch(Exception ex)
        /// </summary>
        [Test]
        public async Task CatchOtherExceptionAndReturnProperResult()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine);

            searchEngine.SetActionsWithRepeatingTheLastOneForever(new TestSearchEngineThrowExceptionAction());

            var searchResults = lookuper.Lookup(TextsForKeyPresses("Jo"));

            await searchResults.ShouldHaveSearchResults(new[] { "<< ERROR >>" });
        }

        /// <summary>
        /// [9]: cancellation
        /// </summary>
        /// <remarks>
        /// NOTE that for implementations below LookuperRx08WithCancellationByResubscribe this test would hang (would never end), hence the <see cref="TimeoutAttribute"/>.
        /// Also NOTE that even LookuperRx08WithCancellationByResubscribe cannot past this test, because it handles cancellation by unsubscribing, and this cannot be tested easily.
        /// </remarks>
        [Test]
        [Timeout(TEST_RUNNING_TIMEOUT_IN_MILLISECONDS)]
        public async Task SearchShouldBeCancellable()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine, timeoutDueTime: TimeSpan.FromMinutes(1));

            searchEngine.SetActionsWithRepeatingTheLastOneForever(new TestSearchEngineInfiniteAction());

            var searchResultAssertTask = lookuper.Lookup(TextsForKeyPresses("Jo"))
                .ShouldComplete();

            await searchEngine.SearchBeginnings.FirstAsync(); // wait for ["Jo"] to be started processing

            lookuper.Cancel();

            await searchResultAssertTask;
        }

        /// <summary>
        /// [9]: cancellation
        /// </summary>
        /// <remarks>
        /// NOTE that for implementations below LookuperRx08WithCancellationByResubscribe this test would hang (would never end), hence the <see cref="TimeoutAttribute"/>.
        /// Also NOTE that even LookuperRx08WithCancellationByResubscribe cannot past this test, because it handles cancellation by unsubscribing, and this cannot be tested easily.
        /// </remarks>
        [Test]
        [Timeout(TEST_RUNNING_TIMEOUT_IN_MILLISECONDS)]
        public async Task SearchShouldBeCancellableAndReturnProperResult()
        {
            var searchEngine = new TestSearchEngine("John", "Joe", "Sarah");
            var lookuper = GetLookuper(searchEngine, timeoutDueTime: TimeSpan.FromMinutes(1));

            searchEngine.SetActionsWithRepeatingTheLastOneForever(new TestSearchEngineInfiniteAction());

            var searchResultAssertTask = lookuper.Lookup(TextsForKeyPresses("Jo"))
                .ShouldHaveSearchResults(new[] {"<< CANCEL >>"});

            await searchEngine.SearchBeginnings.FirstAsync(); // wait for ["Jo"] to be started processing
            lookuper.Cancel();

            await searchResultAssertTask;
        }

        #region Helpers

        private IObservable<string> TextsForKeyPresses(params string[] texts) => texts.ToObservable();

        private IObservable<string> TextsForKeyPressesWithWaitingForEachSearchBeginning(IObservable<string> searchBeginnings, params string[] texts) =>
            TextsForKeyPressesWithWaitingForEachSearchBeginning(searchBeginnings, texts.ToObservable());

        private IObservable<string> TextsForKeyPressesWithWaitingForEachSearchBeginning(IObservable<string> searchBeginnings, IObservable<string> texts) =>
            texts
                .Publish(textsT =>
                    textsT.FirstAsync()
                        .Concat(
                            textsT
                                .Select((item, index) => Observable.Return(item).Zip(searchBeginnings.ElementAt(index), (itemT, _) => itemT))
                                .Concat()
                        )
                );

        #endregion
    }
}
