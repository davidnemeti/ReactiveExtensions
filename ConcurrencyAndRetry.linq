<Query Kind="Program">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
  <Namespace>System.Reactive.Threading.Tasks</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
</Query>

DumpContainer countConcurrency1Container = new DumpContainer(0).Dump("Count of running tasks #1");
DumpContainer countConcurrency2Container = new DumpContainer(0).Dump("Count of running tasks #2");
DumpContainer countConcurrency3Container = new DumpContainer(0).Dump("Count of running tasks #3");
DumpContainer countConcurrencyAllContainer = new DumpContainer(0).Dump("Count of running tasks All");

object locker = new object();
Random random = new Random();

ConcurrentDictionary<int, int> numberToTryCount = new ConcurrentDictionary<int, int>();

enum Example
{
	MergeAllAtOnce,
	MergeFewAtOnce,
	MergeFewAtOnceForTask1ThenMergeFewAtOnceForTask2,
	MergeFewAtOnceForTask1AndTask2,
	MergeFewAtOnceForTask1AndTask2_Variant,
	StagedMergeFewAtOnceForTask1AndTask2,
	SeparatedStagedMergeFewAtOnceForTask1AndTask2,
	MergeFewAtOnceForTask1AndTask2ThenMergeFewAtOnceForTask3,
	MergeFewAtOnceWithRetry,
	MergeFewAtOnceWithRetryAndDelay,
	NonJammerMergeWithRetryAndDelayFewAtOnce,
	ConcatJam,
	ConcatOmit
}

void Main()
{
	Util.AutoScrollResults = false;

	Example exampleToRun = Example.MergeAllAtOnce;	// change this value to run other examples
	
	switch (exampleToRun)
	{
		case Example.MergeAllAtOnce:
			Observable.Range(0, 30)
				.Select(number => Observable.FromAsync(() => CalculateLongRunningTask1(number)))
				.Merge()
				.Dump($"{exampleToRun}");
			break;

		case Example.MergeFewAtOnce:
			Observable.Range(0, 30)
				.Select(number => Observable.FromAsync(() => CalculateLongRunningTask1(number)))
				.Merge(5)
				.Dump($"{exampleToRun}");
			break;

		case Example.MergeFewAtOnceForTask1ThenMergeFewAtOnceForTask2:
			Observable.Range(0, 30)
				.Select(number => Observable.FromAsync(() => CalculateLongRunningTask1(number)))
				.Merge(5)
				.Select(number => Observable.FromAsync(() => CalculateLongRunningTask2(number)))
				.Merge(10)
				.Dump($"{exampleToRun}");
			break;

		case Example.MergeFewAtOnceForTask1AndTask2:
			Observable.Range(0, 30)
				.Select(number => Observable.FromAsync(async () => await CalculateLongRunningTask1(number)))
				.Select(number => Observable.FromAsync(async () => await CalculateLongRunningTask2(await number)))
				.Merge(10)
				.Dump($"{exampleToRun}");
			break;

		case Example.MergeFewAtOnceForTask1AndTask2_Variant:
			Observable.Range(0, 30)
				.Select(number => Observable.FromAsync(async () =>
					{
						var result = await CalculateLongRunningTask1(number);
						return await CalculateLongRunningTask2(result);
					}))
				.Merge(10)
				.Dump($"{exampleToRun}");
			break;

		case Example.StagedMergeFewAtOnceForTask1AndTask2:
			Observable.Range(0, 30)
				.StagedMerge(
					timeSpan: TimeSpan.FromSeconds(5),
					count: 5,
					separateStages: false,
					stage1: number => Observable.FromAsync(() => CalculateLongRunningTask1(number)),
					stage2: number => Observable.FromAsync(() => CalculateLongRunningTask2(number))
					)
				.Dump($"{exampleToRun}");
			break;

		case Example.SeparatedStagedMergeFewAtOnceForTask1AndTask2:
			Observable.Range(0, 30)
				.StagedMerge(
					timeSpan: TimeSpan.FromSeconds(5),
					count: 5,
					separateStages: true,
					stage1: number => Observable.FromAsync(() => CalculateLongRunningTask1(number)),
					stage2: number => Observable.FromAsync(() => CalculateLongRunningTask2(number))
					)
				.Dump($"{exampleToRun}");
			break;

		case Example.MergeFewAtOnceForTask1AndTask2ThenMergeFewAtOnceForTask3:
			Observable.Range(0, 30)
				.Select(number => Observable.FromAsync(async () => await CalculateLongRunningTask1(number)))
				.Select(number => Observable.FromAsync(async () => await CalculateLongRunningTask2(await number)))
				.Merge(10)
				.Select(number => Observable.FromAsync(async () => await CalculateLongRunningTask3(number)))
				.Merge(5)
				.Dump($"{exampleToRun}");
			break;

		case Example.MergeFewAtOnceWithRetry:
			Util.AutoScrollResults = true;

			Observable.Range(0, 30)
				.Select(number =>
					Observable.FromAsync(() => CalculateLongRunningTask1(number, throwException: number < 5 && numberToTryCount.AddOrUpdate(number, 0, (numberT, tryCount) => tryCount + 1) < 3))
						.Retry()
				)
				.Merge(5)
				.Dump($"{exampleToRun}");
			break;

		case Example.MergeFewAtOnceWithRetryAndDelay:
			Util.AutoScrollResults = true;

			Observable.Range(0, 30)
				.Select(number =>
					Observable.FromAsync(() => CalculateLongRunningTask1(number, throwException: number < 5 && numberToTryCount.AddOrUpdate(number, 0, (numberT, tryCount) => tryCount + 1) < 3))
						.RetryWithDelay(TimeSpan.FromSeconds(5))
				)
				.Merge(5)
				.Dump($"{exampleToRun}");
			break;

		case Example.NonJammerMergeWithRetryAndDelayFewAtOnce:
			Util.AutoScrollResults = true;

			Observable.Range(0, 15).Concat(Observable.Range(15, 10).Delay(TimeSpan.FromSeconds(8))).Concat(Observable.Range(25, 5).Delay(TimeSpan.FromSeconds(8)))
				.Select(number => Observable.FromAsync(() => CalculateLongRunningTask1(number, throwException: number < 5 && numberToTryCount.AddOrUpdate(number, 0, (numberT, tryCount) => tryCount + 1) < 2)))
				.MergeWithRetry(5, minRetryDelay: TimeSpan.FromSeconds(3))
				.Dump($"{exampleToRun}");
			break;

		case Example.ConcatJam:
			Util.AutoScrollResults = true;

			Observable.Interval(TimeSpan.FromMilliseconds(100))
				.Timestamp()
				.Select(timestamped => Observable.FromAsync(() => CalculateLongRunningTask1(timestamped)))
				.Concat()
				.Select(item => new { item.result.Timestamp, item.result.Value, item.info1 })
				.Dump($"{exampleToRun}");
			break;

		case Example.ConcatOmit:
			Util.AutoScrollResults = true;

			Observable.Interval(TimeSpan.FromMilliseconds(100))
				.Timestamp()
				.Latest()
				.Select(timestamped => Observable.FromAsync(() => CalculateLongRunningTask1(timestamped)))
				.Concat()
				.Select(item => new { item.result.Timestamp, item.result.Value, item.info1 })
				.Dump($"{exampleToRun}");
			break;
	}
}

void IncreaseConcurrencyCount(DumpContainer countConcurrencyContainer, int increaseAmount = 1)
{
	lock (locker)
	{
		countConcurrencyContainer.Content = (int)countConcurrencyContainer.Content + increaseAmount;
		countConcurrencyAllContainer.Content = (int)countConcurrency1Container.Content + (int)countConcurrency2Container.Content + (int)countConcurrency3Container.Content;
	}
}

void DecreaseConcurrencyCount(DumpContainer countConcurrencyContainer, int increaseAmount = 1)
{
	IncreaseConcurrencyCount(countConcurrencyContainer, -increaseAmount);
}

async Task<(T result, string info1)> CalculateLongRunningTask1<T>(T number, bool throwException = false)
{
	return (
		result: await CalculateLongRunningTask(number, countConcurrency1Container, TimeSpan.FromMilliseconds(random.Next(1000, 2000)), throwException),
		info1: $"thread #{Thread.CurrentThread.ManagedThreadId}"
		);
}

async Task<(T result, string info1, string info2)> CalculateLongRunningTask2<T>((T result, string info1) item)
{
	var (number, info1) = item;

	return (
		result: await CalculateLongRunningTask(number, countConcurrency2Container, TimeSpan.FromMilliseconds(random.Next(2000, 3000))),
		info1,
		info2: $"thread #{Thread.CurrentThread.ManagedThreadId}"
		);
}

async Task<(T result, string info1, string info2, string info3)> CalculateLongRunningTask3<T>((T result, string info1, string info2) item)
{
	var (number, info1, info2) = item;

	return (
		result: await CalculateLongRunningTask(number, countConcurrency3Container, TimeSpan.FromMilliseconds(random.Next(1000, 3000))),
		info1,
		info2,
		info3: $"thread #{Thread.CurrentThread.ManagedThreadId}"
		);
}

async Task<T> CalculateLongRunningTask<T>(T number, DumpContainer dumpContainer, TimeSpan delay, bool throwException = false)
{
	IncreaseConcurrencyCount(dumpContainer);
	try
	{
		await Task.Delay(delay);

		if (throwException)
		{
			ThrowException();
		}

		return number;
	}
	finally
	{
		DecreaseConcurrencyCount(dumpContainer);
	}
	
	void ThrowException()
	{
		string errorMessage = $"ERROR for number '{number}'";
		Util.WithStyle(errorMessage, "color:red").Dump();
		throw new InvalidOperationException(errorMessage);
	}
}

public static class Ext
{
	private static readonly IScheduler DEFAULT_MERGE_WITH_RETRY_SCHEDULER = Scheduler.CurrentThread;

	public static IObservable<T> MergeWithRetry<T>(this IObservable<IObservable<T>> sources, int maxConcurrency, TimeSpan minRetryDelay) =>
		sources.MergeWithRetry(maxConcurrency, minRetryDelay, DEFAULT_MERGE_WITH_RETRY_SCHEDULER);

	public static IObservable<T> MergeWithRetry<T>(this IObservable<IObservable<T>> sources, int maxConcurrency, TimeSpan minRetryDelay, IScheduler scheduler) =>
		sources.MergeWithRetry(maxConcurrency, minRetryDelay, Observable.Never<Unit>(), scheduler);

	public static IObservable<T> MergeWithRetry<T, TUntil>(this IObservable<IObservable<T>> sources, int maxConcurrency, TimeSpan minRetryDelay, IObservable<TUntil> until) =>
		sources.MergeWithRetry(maxConcurrency, minRetryDelay, until, DEFAULT_MERGE_WITH_RETRY_SCHEDULER);

	public static IObservable<T> MergeWithRetry<T, TUntil>(this IObservable<IObservable<T>> sources, int maxConcurrency, TimeSpan minRetryDelay, IObservable<TUntil> until, IScheduler scheduler)
	{
		return Observable.Create<T>(async (observer, cancellationToken) =>
		{
			object processingSourceLock = new object();
			var toBeProcessedSources = new Queue<IObservable<T>>();
			int unprocessedSourceCount = 0;
			int processingSourceCount = 0;
			var sleepingSources = new Subject<IObservable<T>>();
			var processedSources = new Subject<Unit>();
			bool cancelled = false;

			sleepingSources.TakeUntil(until).Delay(minRetryDelay).Subscribe(
				onNext: sleepingSource =>
				{
					lock (processingSourceLock)
					{
						toBeProcessedSources.Enqueue(sleepingSource);
						processSourceIfPossible();
					}
				}
			);

			until.Subscribe(onNext: _ =>
			{
				lock (processingSourceLock)
				{
					cancelled = true;
					completeProcessedIfNotProcessing();
				}
			});

			await sources.ForEachAsync(source =>
				{
					lock (processingSourceLock)
					{
						toBeProcessedSources.Enqueue(source);
						unprocessedSourceCount++;
						processSourceIfPossible();
					}
				},
				cancellationToken
			);

			var unprocessedSourceToAwait = Observable.Empty<Unit>();
			lock (processingSourceLock)
			{
				if (unprocessedSourceCount > 0)
				{
					unprocessedSourceToAwait = processedSources.Take(unprocessedSourceCount).LastOrDefaultAsync();
				}
			}
			await unprocessedSourceToAwait;
			observer.OnCompleted();

			void completeProcessedIfNotProcessing()
			{
				lock (processingSourceLock)
				{
					if (processingSourceCount == 0)
					{
						processedSources.OnCompleted();
					}
				}
			};

			void processSourceIfPossible()
			{
				lock (processingSourceLock)
				{
					if (cancelled)
					{
						return;
					}

					if (processingSourceCount < maxConcurrency && toBeProcessedSources.Count > 0)
					{
						var sourceToProcess = toBeProcessedSources.Dequeue();
						processingSourceCount++;

						// NOTE: The default scheduler should not be Scheduler.Immediate, because it would cause an infinite recursion in case of the onError/onCompleted running synchronously.
						sourceToProcess.SubscribeOn(scheduler).Subscribe(
							onNext: item => observer.OnNext(item),
							onError: ex =>
							{
								lock (processingSourceLock)
								{
									// NOTE: no need to unsubscribe from sourceToProcess, because it has already been done automatically on "error"
									processingSourceCount--;
									if (cancelled)
									{
										completeProcessedIfNotProcessing();
										return;
									}
									else
									{
										sleepingSources.OnNext(sourceToProcess);
										processSourceIfPossible();
									}
								}
							},
							onCompleted: () =>
							{
								lock (processingSourceLock)
								{
									// NOTE: no need to unsubscribe from sourceToProcess, because it has already been done automatically on "completed"
									processingSourceCount--;
									unprocessedSourceCount--;
									processedSources.OnNext(Unit.Default);
									if (cancelled)
									{
										completeProcessedIfNotProcessing();
										return;
									}
									else
									{
										processSourceIfPossible();
									}
								}
							},
							token: cancellationToken
						);
					}
				}
			};
		});
	}

	public static IObservable<T> RetryWithDelay<T>(this IObservable<T> source, TimeSpan delay) =>
		source
			.Catch((Exception ex) => Observable.Throw<T>(ex).DelaySubscription(delay))
			.Retry();

	public static IObservable<TResult1> StagedMerge<TSource, TResult1, TResult2>(this IObservable<TSource> sources, TimeSpan timeSpan, int count, bool separateStages,
		Func<TSource, IObservable<TResult1>> stage1)
	{
		return sources
			.Buffer(timeSpan, count)
			.Select(numbers =>
				numbers
					.Select(stage1)
					.Merge()
			)
			.Concat();
	}

	public static IObservable<TResult2> StagedMerge<TSource, TResult1, TResult2>(this IObservable<TSource> sources, TimeSpan timeSpan, int count, bool separateStages,
		Func<TSource, IObservable<TResult1>> stage1, Func<TResult1, IObservable<TResult2>> stage2)
	{
		return sources
			.Buffer(timeSpan, count)
			.Select(numbers =>
				numbers
					.Select(stage1)
					.MergeForStage(waitForAll: separateStages)
					.Select(stage2)
					.Merge()
			)
			.Concat();
	}

	public static IObservable<TResult3> StagedMerge<TSource, TResult1, TResult2, TResult3>(this IObservable<TSource> sources, TimeSpan timeSpan, int count, bool separateStages,
		Func<TSource, IObservable<TResult1>> stage1, Func<TResult1, IObservable<TResult2>> stage2, Func<TResult2, IObservable<TResult3>> stage3)
	{
		return sources
			.Buffer(timeSpan, count)
			.Select(numbers =>
				numbers
					.Select(stage1)
					.MergeForStage(waitForAll: separateStages)
					.Select(stage2)
					.MergeForStage(waitForAll: separateStages)
					.Select(stage3)
					.Merge()
			)
			.Concat();
	}

	private static IObservable<TSource> MergeForStage<TSource>(this IEnumerable<IObservable<TSource>> sources, bool waitForAll) =>
		MergeForStage(sources.ToObservable(), waitForAll);

	private static IObservable<TSource> MergeForStage<TSource>(this IObservable<IObservable<TSource>> sources, bool waitForAll)
	{
		var mergedSources = sources.Merge();

		return waitForAll
			? mergedSources
				.ToList()
				.SelectMany(numbersT => numbersT)
			: mergedSources;
	}
}