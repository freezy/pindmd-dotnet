using System;
using System.IO;
using System.Reactive;
using System.Reactive.Subjects;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibDmd.Common;
using LibDmd.Frame;

namespace LibDmd.Input.FileSystem
{
	public class ImageSourceGray2 : ImageSource, IGray2Source
	{
		private readonly DmdFrame _dmdFrame = new DmdFrame();

		public IObservable<DmdFrame> GetGray2Frames() => _frames;

		private readonly BehaviorSubject<DmdFrame> _frames;

		public ImageSourceGray2(BmpFrame frame)
		{
			SetDimensions(frame.Dimensions);
			_frames = new BehaviorSubject<DmdFrame>(_dmdFrame.Update(frame.ConvertToGray2()));
		}
	}

	public class ImageSourceGray4 : ImageSource, IGray4Source
	{
		private readonly DmdFrame _dmdFrame = new DmdFrame();

		public IObservable<DmdFrame> GetGray4Frames() => _frames;

		private readonly BehaviorSubject<DmdFrame> _frames;

		public ImageSourceGray4(BmpFrame frame)
		{
			SetDimensions(frame.Dimensions);
			_frames = new BehaviorSubject<DmdFrame>(_dmdFrame.Update(frame.ConvertToGray4()));
		}
	}

	public class ImageSourceColoredGray2 : ImageSource, IColoredGray2Source
	{
		public IObservable<ColoredFrame> GetColoredGray2Frames() => _frames;

		private readonly BehaviorSubject<ColoredFrame> _frames;

		public ImageSourceColoredGray2(BmpFrame frame)
		{
			SetDimensions(frame.Dimensions);
			var coloredFrame = new ColoredFrame(frame.Dimensions,
				FrameUtil.Split(frame.Dimensions, 2, ImageUtil.ConvertToGray2(frame.Bitmap)),
				new [] { Colors.Black, Colors.Red, Colors.Green, Colors.Blue }
			);
			_frames = new BehaviorSubject<ColoredFrame>(coloredFrame);
		}
	}

	public class ImageSourceColoredGray4 : ImageSource, IColoredGray4Source
	{
		public IObservable<ColoredFrame> GetColoredGray4Frames() => _frames;

		private readonly BehaviorSubject<ColoredFrame> _frames;

		public ImageSourceColoredGray4(BmpFrame frame)
		{
			SetDimensions(frame.Dimensions);
			var coloredFrame = new ColoredFrame(frame.Dimensions,
				FrameUtil.Split(frame.Dimensions, 4, ImageUtil.ConvertToGray4(frame.Bitmap)),
				new[] {
					Colors.Black, Colors.Blue, Colors.Purple, Colors.DimGray,
					Colors.Green, Colors.Brown, Colors.Red, Colors.Gray,
					Colors.Tan, Colors.Orange, Colors.Yellow, Colors.LightSkyBlue,
					Colors.Cyan, Colors.LightGreen, Colors.Pink, Colors.White,
				}
			);
			_frames = new BehaviorSubject<ColoredFrame>(coloredFrame);
		}
	}

	public class ImageSourceBitmap : ImageSource, IBitmapSource
	{
		public IObservable<BmpFrame> GetBitmapFrames() => _frames;

		private readonly BehaviorSubject<BmpFrame> _frames;

		public ImageSourceBitmap(BmpFrame frame)
		{
			SetDimensions(frame.Dimensions);
			_frames = new BehaviorSubject<BmpFrame>(frame);
		}

		public ImageSourceBitmap(string fileName)
		{
			if (!File.Exists(fileName)) {
				throw new FileNotFoundException("Cannot find file \"" + fileName + "\".");
			}

			try {
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(Path.IsPathRooted(fileName) ? fileName : Path.Combine(Directory.GetCurrentDirectory(), fileName));
				bmp.EndInit();

				var frame = new BmpFrame(bmp);
				SetDimensions(frame.Dimensions);
				_frames = new BehaviorSubject<BmpFrame>(frame);

			} catch (UriFormatException) {
				throw new WrongFormatException($"Error parsing file name \"{fileName}\". Is this a path on the file system?");

			} catch (NotSupportedException e) {
				if (e.Message.Contains("No imaging component suitable"))
				{
					throw new WrongFormatException($"Could not determine image format. Are you sure {fileName} is an image?");
				}
				throw new Exception("Error instantiating image source.", e);
			}
		}
	}

	public abstract class ImageSource : AbstractSource
	{
		public override string Name { get; } = "Image Source";

		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();
	}

	public class WrongFormatException : Exception
	{
		public WrongFormatException(string message) : base(message)
		{
		}
	}
}
