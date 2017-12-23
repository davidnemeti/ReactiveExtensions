<Query Kind="Program">
  <NuGetReference>Microsoft.Kinect</NuGetReference>
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>Microsoft.Kinect</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive</Namespace>
</Query>

void Main()
{
	var kinect = KinectSensor.GetDefault();
	
	kinect.Open();
	
	var reader = kinect.BodyFrameSource.OpenReader();
	
	var bodyStream = Observable.FromEventPattern<BodyFrameArrivedEventArgs>(
		addHandler: handler => reader.FrameArrived += handler,
		removeHandler: handler => reader.FrameArrived -= handler
		)
		.Select(frameEvent =>
	    {
	        var bodies = new Body[kinect.BodyFrameSource.BodyCount];
	        using (var frame = frameEvent.EventArgs.FrameReference.AcquireFrame())
	            frame.GetAndRefreshBodyData(bodies);
	
	        return bodies.SingleOrDefault(b => b.IsTracked);
		})
		.Where(body => body != null);
	
	var handPositions =
		from body in bodyStream
		let hand = body.Joints[JointType.HandRight]
		where hand.TrackingState != TrackingState.NotTracked
		select hand.Position;

	Func<IObservable<CameraSpacePoint>, IObservable<Direction>> detectRapidHandMovements =
		handPositionsT => handPositionsT.DetectRapidHandMovements(durationThreshold: TimeSpan.FromMilliseconds(200), distanceThreshold: 0.1f);

	var wavesByDuration = handPositions.DetectWavingByRapidMovementDuration(
		detectRapidHandMovements,
		rapidMovementBufferCount: 5,
		rapidMovementDurationThreshold: TimeSpan.FromSeconds(3)
		)
		.Select((wavingDuration, wavingIndex) => $"Waving #{wavingIndex} for {Math.Round(wavingDuration.TotalSeconds)}s");

	Util.AutoScrollResults = false;
	Util.HorizontalRun("Hand Positions X,Rapid Hand Movements,Waves,Waves2",
		handPositions.Select(position => ToStringShort(position.X)).ReverseDumpContainer(limit: 10),
		detectRapidHandMovements(handPositions).ReverseDumpContainer(limit: 10),
		wavesByDuration.ReverseDumpContainer(limit: 10)
		).Dump();

	Util.KeepRunning();

	string ToStringShort(float number) => $"{number,6:0.00}";
}

enum Direction
{
	Left,
	Right
}

public class Foo
{
}

static class Extensions
{
	public static IObservable<Direction> DetectRapidHandMovements(
		this IObservable<CameraSpacePoint> handPositions,
		TimeSpan durationThreshold,
		float distanceThreshold
		) =>
		from handPositionBuffer in handPositions.Buffer(durationThreshold)
		where handPositionBuffer.Any()
		let xFirst = handPositionBuffer.First().X
		let xLast = handPositionBuffer.Last().X
		let direction =	Math.Abs(xLast - xFirst) < distanceThreshold	?	(Direction?)null :
						xFirst < xLast 									?	Direction.Right :
																			Direction.Left
		where direction.HasValue
		select direction.Value;

	public static IObservable<TimeSpan> DetectWavingByRapidMovementDuration(
		this IObservable<CameraSpacePoint> handPositions,
		Func<IObservable<CameraSpacePoint>, IObservable<Direction>> detectRapidHandMovements,
		int rapidMovementBufferCount,
		TimeSpan rapidMovementDurationThreshold
		) =>
		detectRapidHandMovements(handPositions)
			.DistinctUntilChanged()
			.Rate(rapidMovementDurationThreshold, rapidMovementBufferCount)
			.Select(buffer => buffer.Last().Timestamp - buffer.First().Timestamp);

	public static IObservable<RatedBuffer<Timestamped<T>>> RateAll<T>(this IObservable<T> source, TimeSpan timeSpan, int count) =>
		source
			.Timestamp()
			.Buffer(count: count, skip: 1)
			.Where(buffer => buffer.Any())
			.Select(buffer => new RatedBuffer<Timestamped<T>>(buffer, isFrequent: buffer.Last().Timestamp - buffer.First().Timestamp <= timeSpan));

	public static IObservable<IList<Timestamped<T>>> Rate<T>(this IObservable<T> source, TimeSpan timeSpan, int count) =>
		source
			.RateAll(timeSpan, count)
			.Where(ratedBuffer => ratedBuffer.IsFrequent)
			.Select(ratedBuffer => ratedBuffer.Buffer);

	public static DumpContainer ReverseDumpContainer<T>(this IObservable<T> source, int? limit = null)
	{
		var dumpContainer = new DumpContainer();
		dumpContainer.Style = "color:green";
		bool isLimitReached = false;

		source
			.Subscribe(
				onNext: item =>
				{
					AddItem(item);
				},
				onError: ex =>
				{
					dumpContainer.Style = "color:red";
					AddItem(ex);
				},
				onCompleted: () => dumpContainer.Style = "color:blue"
			);

		return dumpContainer;

		void AddItem(object item)
		{
			var list = EnsureContentList();
			list.Insert(0, item);
			if (isLimitReached || list.Count > limit)
			{
				if (isLimitReached)
				{
					list.RemoveAt(list.Count - 2);
				}
				else
				{
					isLimitReached = true;
					list.RemoveAt(list.Count - 1);
					list.Add("...");
				}
			}
			dumpContainer.Refresh();

			IList<object> EnsureContentList()
			{
				var listT = dumpContainer.Content as IList<object>;
				if (listT == null)
				{
					listT = new List<object>();
					dumpContainer.Content = listT;
				}
				return listT;
			}
		}
	}
}

public struct RatedBuffer<T>
{
	public IList<T> Buffer { get; }
	public bool IsFrequent { get; }

	public RatedBuffer(IList<T> buffer, bool isFrequent)
	{
		Buffer = buffer;
		IsFrequent = isFrequent;
	}
}
