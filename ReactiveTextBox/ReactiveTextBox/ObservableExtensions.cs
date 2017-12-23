using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace ReactiveTextBox
{
    public static class ObservableExtensions
    {
        public static IObservable<T> DoExtended<T>(
            this IObservable<T> source,
            Action onSubscribe,
            Action<T> onNext = null,
            Action onCompleted = null,
            Action<Exception> onError = null,
            Action<bool> onFinally = null
        ) =>
            source.DoExtended(
                onSubscribe: () =>
                {
                    onSubscribe();
                    return Unit.Default;
                },
                onNext: (item, unit) => onNext?.Invoke(item),
                onCompleted: unit => onCompleted?.Invoke(),
                onError: (ex, unit) => onError?.Invoke(ex),
                onFinally: (terminated, unit) => onFinally?.Invoke(terminated)
            );

        public static IObservable<T> DoExtended<T, TResource>(
            this IObservable<T> source,
            Func<TResource> onSubscribe,
            Action<T, TResource> onNext = null,
            Action<TResource> onCompleted = null,
            Action<Exception, TResource> onError = null,
            Action<bool, TResource> onFinally = null
        )
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (onSubscribe == null) throw new ArgumentNullException(nameof(onSubscribe));

            return Observable.Create<T>(observer =>
            {
                TResource resource;
                try
                {
                    resource = onSubscribe();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                    return Disposable.Empty;
                }

                bool terminated = false;

                return new CompositeDisposable(
                    source.Subscribe(
                        onNext: item =>
                        {
                            try
                            {
                                onNext?.Invoke(item, resource);
                            }
                            catch (Exception ex)
                            {
                                observer.OnError(ex);
                                return;
                            }
                            observer.OnNext(item);
                        },
                        onError: ex =>
                        {
                            terminated = true;
                            try
                            {
                                onError?.Invoke(ex, resource);
                            }
                            catch (Exception exInternal)
                            {
                                observer.OnError(new AggregateException(ex, exInternal));
                                return;
                            }
                            observer.OnError(ex);
                        },
                        onCompleted: () =>
                        {
                            terminated = true;
                            try
                            {
                                onCompleted?.Invoke(resource);
                            }
                            catch (Exception ex)
                            {
                                observer.OnError(ex);
                                return;
                            }
                            observer.OnCompleted();
                        }
                    ),
                    Disposable.Create(() =>
                    {
                        try
                        {
                            onFinally?.Invoke(terminated, resource);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    })
                );
            });
        }

        public static IObservable<T> ObserveOnIfContextIsNotNull<T>(this IObservable<T> source, SynchronizationContext context) =>
            context != null
                ? source.ObserveOn(context)
                : source;
    }
}
