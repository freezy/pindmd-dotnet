using System;
using System.Reactive;
using System.Reactive.Subjects;
using LibDmd.Frame;

namespace LibDmd.Input.PinMame
{
	/// <summary>
	/// Receives 4-bit frames from VPM and forwards them to the observable
	/// after dropping duplicates.
	/// </summary>
	public class VpmGray4Source : AbstractSource, IGray4Source
	{
		public override string Name { get; } = "VPM 4-bit Source";

		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();

		private readonly Subject<DmdFrame> _framesGray4 = new Subject<DmdFrame>();
		private readonly BehaviorSubject<FrameFormat> _lastFrameFormat;

		public VpmGray4Source(BehaviorSubject<FrameFormat> lastFrameFormat)
		{
			_lastFrameFormat = lastFrameFormat;
		}

		public void NextFrame(DmdFrame frame)
		{
			SetDimensions(frame.Dimensions);
			_framesGray4.OnNext(frame);
			_lastFrameFormat.OnNext(FrameFormat.Gray4);
		}

		public IObservable<DmdFrame> GetGray4Frames()
		{
			return _framesGray4;
		}
	}
}
