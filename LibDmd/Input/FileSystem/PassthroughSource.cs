using System;
using System.Reactive;
using System.Reactive.Subjects;
using LibDmd.Frame;

namespace LibDmd.Input.FileSystem
{
	/// <summary>
	/// An input source just contains observables with all subjects.
	/// </summary>
	public class PassthroughSource : AbstractSource, IGray2Source, IGray4Source, IRgb24Source, IColoredGray2Source, IColoredGray4Source, IBitmapSource
	{
		public override string Name { get; }

		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();

		public readonly Subject<DmdFrame> FramesGray2 = new Subject<DmdFrame>();
		public readonly Subject<DmdFrame> FramesGray4 = new Subject<DmdFrame>();
		public readonly Subject<DmdFrame> FramesRgb24 = new Subject<DmdFrame>();
		public readonly Subject<ColoredFrame> FramesColoredGray2 = new Subject<ColoredFrame>();
		public readonly Subject<ColoredFrame> FramesColoredGray4 = new Subject<ColoredFrame>();
		public readonly Subject<BmpFrame> FramesBitmap = new Subject<BmpFrame>();

		public PassthroughSource(string name)
		{
			Name = name;
		}

		public IObservable<DmdFrame> GetGray2Frames()
		{
			return FramesGray2;
		}

		public IObservable<DmdFrame> GetGray4Frames()
		{
			return FramesGray4;
		}

		public IObservable<ColoredFrame> GetColoredGray2Frames()
		{
			return FramesColoredGray2;
		}

		public IObservable<ColoredFrame> GetColoredGray4Frames()
		{
			return FramesColoredGray4;
		}

		public IObservable<DmdFrame> GetRgb24Frames()
		{
			return FramesRgb24;
		}

		public IObservable<BmpFrame> GetBitmapFrames()
		{
			return FramesBitmap;
		}
	}
}
