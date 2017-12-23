using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTextBox
{
    public class SearchEngine : ISearchEngine
    {
        private static readonly IReadOnlyList<string> names = new[] {
            "John",
            "Bill",
            "Joe",
            "Jenna",
            "Sue",
            "Sarah",
            "Samantha",
        };

        private static bool _shouldErrorForDemo = true;
        private static bool _shouldTimeoutForDemo = true;

        public async Task<string[]> Search(string text, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            #region Calculating delay and exception throwing

            const int textLengthForSwitchingError = 5;
            const int textLengthForSwitchingDelay = 7;
            TimeSpan delay;
            switch (text.Length)
            {
                case int length when length < 4:
                    delay = TimeSpan.FromSeconds(1);
                    break;

                case 4:
                    delay = TimeSpan.FromMilliseconds(10);
                    break;

                case textLengthForSwitchingError:
                    if (_shouldErrorForDemo)
                    {
                        _shouldErrorForDemo = !_shouldErrorForDemo;
                        throw new ApplicationException("Fatal communication error");
                    }
                    else
                    {
                        _shouldErrorForDemo = !_shouldErrorForDemo;
                        delay = TimeSpan.FromMilliseconds(10);
                    }
                    break;

                case 6:
                    throw new ApplicationException("Fatal communication error");

                case textLengthForSwitchingDelay:
                    delay = _shouldTimeoutForDemo ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(1);
                    _shouldTimeoutForDemo = !_shouldTimeoutForDemo;
                    break;

                default:
                    delay = TimeSpan.FromSeconds(5);
                    break;
            }

            switch (text.Length)
            {
                case textLengthForSwitchingError:
                case textLengthForSwitchingDelay:
                    break;

                default:
                    _shouldErrorForDemo = true;
                    _shouldTimeoutForDemo = true;
                    break;
            }

            #endregion

            await Task.Delay(delay, ct);

            var searchResult =
                from name in names
                where name.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1
                orderby name
                select name;

            return searchResult.ToArray();
        }
    }
}
