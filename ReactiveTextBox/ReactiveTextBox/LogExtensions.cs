using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReactiveTextBox
{
    public static class LogExtensions
    {
        public static IObservable<T> __DO_FOR_CONTROL<T>(this IObservable<T> texts, Action<T> doForControl) =>
            SynchronizationContext.Current != null
                ? texts
                    .ObserveOn(SynchronizationContext.Current)
                    .Do(onNext: doForControl)
                    .ObserveOn(Scheduler.Default)
                : texts;

        public static IObservable<string> __LOG_RAW_TEXT_FOR_DEMO(this IObservable<string> texts) =>
            texts.Do(onNext: text => Logging.rawTextsLog.OnNext(text));

        public static IObservable<string> __LOG_TEXT_FOR_DEMO(this IObservable<string> texts) =>
            texts.Do(onNext: text => Logging.textsLog.OnNext(text));

        public static IObservable<T> __LOG_FOR_DEMO<T>(this IObservable<T> source, string message) =>
            source.Do(onNext: text => Logging.callsLog.OnNext($"{message}"));

        public static IObservable<T> __LOG_ERROR_FOR_DEMO<T>(this IObservable<T> source, string message) =>
            source.Do(
                onNext: text => { },
                onError: ex => Logging.callsLog.OnNext($"{message} ({ToStringShort(ex)})")
            );

        public static IObservable<T> __LOG_ERROR_FOR_DEMO<T>(this IObservable<T> source, string message, Exception ex) =>
            source.Do(onNext: text => Logging.callsLog.OnNext($"{message} ({ToStringShort(ex)})"));

        public static IObservable<T> __LOG_TIMEINTERVAL_FOR_DEMO<T>(this IObservable<T> source, string message) =>
            source.DoExtended(
                onSubscribe: () =>
                {
                    Logging.callsLog.OnNext($"BEGIN {message}");
                    return Stopwatch.StartNew();
                },
                onCompleted: stopwatch => Logging.callsLog.OnNext($"END {message} ({stopwatch.ElapsedMilliseconds} ms)"),
                onError: (ex, stopwatch) => Logging.callsLog.OnNext($"ERROR {message} ({ToStringShort(ex)}) ({stopwatch.ElapsedMilliseconds} ms)"),
                onFinally: (terminated, stopwatch) =>
                {
                    if (!terminated)
                    {
                        Logging.callsLog.OnNext($"CANCEL {message} ({stopwatch.ElapsedMilliseconds} ms)");
                    }
                }
            );

        public static void __LOG_RAW_TEXT_FOR_DEMO(string text)
        {
            Logging.rawTextsLog.OnNext(text);
        }

        public static void __LOG_TEXT_FOR_DEMO(string text)
        {
            Logging.textsLog.OnNext(text);
        }

        public static void __LOG_FOR_DEMO(string text)
        {
            Logging.callsLog.OnNext(text);
        }

        public static void __LOG_ERROR_FOR_DEMO(string message, Exception ex)
        {
            Logging.callsLog.OnNext($"{message} ({ToStringShort(ex)})");
        }

        private static string ToStringShort(Exception ex) => $"{ex.GetType()}: {ex.Message}";
    }
}
