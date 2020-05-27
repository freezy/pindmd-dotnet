using System;
using System.Reactive;
using System.Reactive.Subjects;
using LibDmd.Common;
using LibDmd.Frame;

namespace LibDmd.Input.PinMame
{
	/// <summary>
	/// Receives RGB24 frames from VPM and forwards them to the observable
	/// after dropping duplicates.
	/// </summary>
	public class VpmRgb24Source : AbstractSource, IRgb24Source
	{
		public override string Name { get; } = "VPM RGB24 Source";

		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();

		private readonly Subject<DmdFrame> _framesRgb24 = new Subject<DmdFrame>();
		private byte[] _lastFrame;
		private readonly BehaviorSubject<FrameFormat> _lastFrameFormat;

		public VpmRgb24Source(BehaviorSubject<FrameFormat> lastFrameFormat)
		{
			_lastFrameFormat = lastFrameFormat;
		}

		public void NextFrame(DmdFrame frame)
		{
			if (_lastFrameFormat.Value == FrameFormat.Rgb24 && _lastFrame != null && FrameUtil.CompareBuffers(frame.Data, 0, _lastFrame, 0, frame.Data.Length)) {
				// identical frame, drop.
				return;
			}
			if (_lastFrame?.Length != frame.Data.Length) {
				_lastFrame = new byte[frame.Data.Length];
			}
			SetDimensions(frame.Dimensions);
			_framesRgb24.OnNext(frame);
			Buffer.BlockCopy(frame.Data, 0, _lastFrame, 0, frame.Data.Length);
			_lastFrameFormat.OnNext(FrameFormat.Rgb24);
		}

		public IObservable<DmdFrame> GetRgb24Frames()
		{
			return _framesRgb24;
		}
	}
}
