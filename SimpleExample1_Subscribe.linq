<Query Kind="Statements">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

var source = Observable.Interval(TimeSpan.FromSeconds(1));

using (var subscription = source.Subscribe(onNext: number => Console.WriteLine($"Number: {number}")))
{
	Util.ReadLine();
}
