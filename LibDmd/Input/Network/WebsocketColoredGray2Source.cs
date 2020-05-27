using System;
using System.Reactive;
using System.Reactive.Subjects;
using LibDmd.Frame;

namespace LibDmd.Input.Network
{
	public class WebsocketColoredGray2Source : AbstractSource, IColoredGray2Source
	{
		public override string Name => "Websocket Colored 2-bit Source";

		IObservable<Unit> ISource.OnResume => _onResume;
		IObservable<Unit> ISource.OnPause => _onPause;

		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();

		public readonly Subject<ColoredFrame> FramesColoredGray2 = new Subject<ColoredFrame>();

		public IObservable<ColoredFrame> GetColoredGray2Frames()
		{
			return FramesColoredGray2;
		}
	}
}
