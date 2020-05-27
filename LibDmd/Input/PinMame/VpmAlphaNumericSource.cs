using System;
using System.Reactive;
using System.Reactive.Subjects;
using LibDmd.Frame;

namespace LibDmd.Input.PinMame
{
	/// <summary>
	/// Receives alphanumeric frames from VPM and forwards them to the observable
	/// after dropping duplicates.
	/// </summary>
	public class VpmAlphaNumericSource : AbstractSource, IAlphaNumericSource
	{
		public override string Name { get; } = "VPM Alpha Numeric Source";

		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();

		private readonly ISubject<AlphaNumericFrame> _framesAlphaNumeric;
		private readonly BehaviorSubject<FrameFormat> _lastFrameFormat;

		public VpmAlphaNumericSource(AlphaNumericFrame initialFrame)
		{
			_framesAlphaNumeric = new BehaviorSubject<AlphaNumericFrame>(initialFrame);
		}

		public VpmAlphaNumericSource(BehaviorSubject<FrameFormat> lastFrameFormat)
		{
			_framesAlphaNumeric =  new Subject<AlphaNumericFrame>();
			_lastFrameFormat = lastFrameFormat;
		}

		public void NextFrame(AlphaNumericFrame frame)
		{
			_framesAlphaNumeric.OnNext(frame);
			_lastFrameFormat.OnNext(FrameFormat.AlphaNumeric);
		}

		public IObservable<AlphaNumericFrame> GetAlphaNumericFrames()
		{
			return _framesAlphaNumeric;
		}
	}
}
