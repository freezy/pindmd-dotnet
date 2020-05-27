using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Media.Imaging;
using LibDmd.Common;
using LibDmd.Frame;
using LibDmd.Processor;

namespace LibDmd.Input.ScreenGrabber
{
	/// <summary>
	/// A screen grabber that captures a portion of the desktop given by
	/// position and dimensions.
	/// </summary>
	public class ScreenGrabber : AbstractSource, IBitmapSource
	{
		public override string Name { get; } = "Screen Grabber";

		public readonly List<AbstractProcessor> Processors = new List<AbstractProcessor>();
		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		public double FramesPerSecond { get; set; } = 30;
		public int Left { get; set; }
		public int Top { get; set; }
		public int Width = 128;
		public int Height = 32;

		public Dimensions DestinationDimensions {
			get { return _destDimensions; }
			set {
				_destDimensions = value;
				SetDimensions(_destDimensions);
			}
		}

		private Dimensions _destDimensions = new Dimensions(128, 32);

		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();

		private readonly BmpFrame _frame = new BmpFrame();
		private IObservable<BmpFrame> _frames;

		public IObservable<BmpFrame> GetBitmapFrames()
		{
			var enabledProcessors = Processors.Where(processor => processor.Enabled);
			return _frames ?? (
				_frames = Observable
					.Interval(TimeSpan.FromMilliseconds(1000 / FramesPerSecond))
					.Select(x => CaptureImage())
					.Select(bmp => enabledProcessors.Aggregate(bmp, (currentBmp, processor) => processor.Process(currentBmp)))
					.Select(bmp => {
						if (!_destDimensions.IsFlat) {
							return _frame.Update(TransformationUtil.Transform(bmp, _destDimensions, ResizeMode.Stretch, false, false));
						}
						return _frame.Update(bmp);
					})
					.Publish()
					.RefCount()
			);
		}

		public void Move(Rectangle rect)
		{
			Left = rect.X;
			Top = rect.Y;
			Width = rect.Width;
			Height = rect.Height;
			SetDimensions(new Dimensions(rect.Width, rect.Height));
		}

		private BitmapSource CaptureImage()
		{
			return NativeCapture.GetDesktopBitmap(Left, Top, Width, Height);
		}
	}
}
