using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Media.Imaging;
using LibDmd.Common;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Input.FileSystem
{
	public class GifSource : AbstractSource, IBitmapSource
	{
		public override string Name { get; } = "GIF Source";

		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();

		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		private readonly BmpFrame _frame = new BmpFrame();
		private readonly IObservable<BmpFrame> _frames;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public GifSource(string fileName)
		{
			if (!File.Exists(fileName)) {
				throw new FileNotFoundException("Cannot find file \"" + fileName + "\".");
			}
			var gif = Image.FromFile(fileName);
			var dim = new FrameDimension(gif.FrameDimensionsList[0]);
			var frameCount = gif.GetFrameCount(dim);

			SetDimensions(new Dimensions(gif.Width, gif.Height));

			if (ImageAnimator.CanAnimate(gif)) {
				var gifFrames = new GifFrame[frameCount];
				var index = 0;
				var time = 0;
				Logger.Info("Reading {0} frames from {1}...", frameCount, fileName);
				for (var i = 0; i < frameCount; i++) {
					var delay = BitConverter.ToInt32(gif.GetPropertyItem(20736).Value, index) * 10;
					gif.SelectActiveFrame(dim, i);
					gifFrames[i] = new GifFrame(ImageUtil.ConvertToBitmap(gif), time);
					index += 4;
					time += delay;
				}

				_frames = gifFrames
					.ToObservable()
					.Delay(gifFrame => Observable.Timer(TimeSpan.FromMilliseconds(gifFrame.Time)))
					.Select(gifFrame => _frame.Update(gifFrame.Bitmap));

				// is looped?
				if (BitConverter.ToInt16(gif.GetPropertyItem(20737).Value, 0) != 1) {
					Logger.Info("GIF animation is looped.");
					_frames = _frames.Repeat();
				}

				_frames = _frames.Publish().RefCount();

			} else {
				_frames = new BehaviorSubject<BmpFrame>(new BmpFrame(ImageUtil.ConvertToBitmap(gif)));
			}
		}

		public IObservable<BmpFrame> GetBitmapFrames()
		{
			return _frames;
		}

		private class GifFrame
		{
			public readonly BitmapSource Bitmap;
			public readonly int Time;
			public GifFrame(BitmapSource bmp, int time) {
				Bitmap = bmp;
				Time = time;
			}
		}
	}
}
