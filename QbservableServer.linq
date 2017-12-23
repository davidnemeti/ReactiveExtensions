<Query Kind="Statements">
  <NuGetReference>Qactive.Providers.Tcp</NuGetReference>
  <Namespace>Qactive</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

var source = Observable.Interval(TimeSpan.FromMilliseconds(100))
	.Select(number => Tuple.Create($"Message: '{number}'", number));

var service = source
	.Do(
		item => $"New sequence item: {item}".Dump(),
		ex => Console.WriteLine("Error in sequence: {0}", ex.Message),
		() => Console.WriteLine("Sequence completed.")
	)
	.ServeQbservableTcp(new IPEndPoint(IPAddress.Any, 3205), QbservableServiceOptions.Unrestricted)
	.Do(
		client => Console.WriteLine("Client shutdown."),
		ex => Console.WriteLine("Fatal error: {0}", ex.Message),
		() => Console.WriteLine("This will never be printed because a service host never completes.")
	);

service.Dump(3);
