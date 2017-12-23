<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Security.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\Accessibility.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Deployment.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.Formatters.Soap.dll</Reference>
  <NuGetReference>Microsoft.Kinect</NuGetReference>
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>Microsoft.Kinect</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
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
	
	var handPositionsFromKinect =
		from body in bodyStream
		let hand = body.Joints[JointType.HandRight]
		where hand.TrackingState != TrackingState.NotTracked
		select hand.Position.X;

	var handPositionsFromMouse =
		Observable.Interval(TimeSpan.FromMilliseconds(10))
			.Select(_ => (float)Cursor.Position.X / Screen.PrimaryScreen.WorkingArea.Width)
			.DistinctUntilChanged()
			.Skip(1);

	var handPositions = handPositionsFromKinect.Merge(handPositionsFromMouse);

	#region Rapid Hand Movements

	var rapidHandMovements =
	(
		from handPositionBuffer in handPositions.Buffer(timeSpan: TimeSpan.FromMilliseconds(100))
		where handPositionBuffer.Any()
		let xFirst = handPositionBuffer.First()
		let xLast = handPositionBuffer.Last()
		let direction =	Math.Abs(xLast - xFirst) < 0.1	?	Direction.None	:
						xFirst < xLast					? 	Direction.Right	:
															Direction.Left
		where direction != Direction.None
		select direction
	).Timestamp();

	#endregion

	#region Waves

	var waves =
		rapidHandMovements
			.DistinctUntilChanged()
			.Timestamp()
			.Buffer(count: 5, skip: 1)
			.Where(buffer => buffer.Last().Timestamp - buffer.First().Timestamp < TimeSpan.FromSeconds(2))
			.Timestamp()
			.Select((buffer, index) => $"Waving #{index} {buffer.Timestamp.TimeOfDay}");
	
//	var waves =
//		rapidHandMovements
//			.DistinctUntilChanged()
//			.Buffer(timeSpan: TimeSpan.FromSeconds(2), timeShift: TimeSpan.FromSeconds(1))
//			.Where(buffer => buffer.Count >= 5)
//			.Select((buffer, index) => $"Waving #{index}");

	#endregion

	Util.AutoScrollResults = false;
	Util.HorizontalRun("Hand Positions,Rapid Hand Movements,Waves",
		handPositions.Select(x => ToStringShort(x)).ReverseDumpContainer(10),
		rapidHandMovements.ReverseDumpContainer(10),
		waves.ReverseDumpContainer(10)
		).Dump();

	Util.KeepRunning();

	string ToStringShort(float number) => $"{number,6:0.00}";
}

enum Direction
{
	None,
	Left,
	Right
}

static class Extensions
{
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