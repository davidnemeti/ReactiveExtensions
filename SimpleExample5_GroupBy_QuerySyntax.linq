<Query Kind="Expression">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

from number in Observable.Interval(TimeSpan.FromSeconds(1))
group $"The number is '{number}'" by number % 3