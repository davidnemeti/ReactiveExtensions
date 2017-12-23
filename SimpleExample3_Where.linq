<Query Kind="Expression">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

Observable.Interval(TimeSpan.FromSeconds(1))
	.Where(number => number % 3 == 0)
	.Select(number => new
	{
		Message = $"Original number: '{number}'",
		FinalNumber = number * 2,
		Thread = $"thread #{Thread.CurrentThread.ManagedThreadId}"
	}
	)
	.Take(5)