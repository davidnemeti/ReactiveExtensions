<Query Kind="Statements">
  <NuGetReference>Qactive.Providers.Tcp</NuGetReference>
  <Namespace>Qactive</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

Util.AutoScrollResults = false;

//var client = new TcpQbservableClient<Tuple<string, long>>(new IPEndPoint(IPAddress.Parse("10.44.19.8"), 3205));
var client = new TcpQbservableClient<Tuple<string, long>>(new IPEndPoint(IPAddress.Loopback, 3205));

// After commenting out ".AsObservable()", you will get a real IQbservable.
// In this case:
//   1. The network traffic will be decreased, because the server will only send the desired items. (You can check this with Wireshark.)
//   2. The "Console.WriteLine" command will be running on the server, not on the client! (You can check the server's output.)

var query =
	from item in client.Query()
		.AsObservable()		// NOTE: comment this line out to get a real IQbservable
		.Do(value => Console.WriteLine($"I am running 'Console.WriteLine': {value}"))
	where item.Item2 % 30 == 0
	select item;

query.Take(10).Dump();
