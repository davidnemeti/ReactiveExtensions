<Query Kind="Expression">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

Observable.Interval(TimeSpan.FromSeconds(1))
	.Window(count: 5, skip: 2)
	.Take(5)