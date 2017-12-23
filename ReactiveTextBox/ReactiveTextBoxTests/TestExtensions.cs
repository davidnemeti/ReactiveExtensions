using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;

namespace ReactiveTextBoxTests
{
    public static class TestExtensions
    {
        public static async Task ShouldFinishWithSearchResult(this IObservable<string[]> source, params string[] expectedFinalSearchResult)
        {
            string[] actualFinalSearchResult = await source.LastAsync();
            actualFinalSearchResult.ShouldAllBeEquivalentTo(expectedFinalSearchResult);
        }

        public static async Task ShouldHaveSearchResults(this IObservable<string[]> source, params string[][] expectedFinalSearchResults)
        {
            string[][] actualFinalSearchResults = await source.ToArray();
            actualFinalSearchResults.ShouldAllBeEquivalentTo(expectedFinalSearchResults);
        }

        public static async Task ShouldComplete(this IObservable<string[]> source)
        {
            await source.LastOrDefaultAsync();
        }
    }
}
