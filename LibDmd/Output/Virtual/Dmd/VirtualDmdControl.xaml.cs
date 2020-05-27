using System;
using System.Threading.Tasks;
using System.Windows.Media;
using LibDmd.Common;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Output.Virtual.Dmd
{
	/// <summary>
	/// Interaction logic for VirtualDmdControl.xaml
	/// </summary>
	public partial class VirtualDmdControl : IRgb24Destination, IBitmapDestination, IResizableDestination, IVirtualControl
	// these others are for debugging purpose. basically you can make the virtual dmd
	// behave like any other display by adding/removing interfaces
	// standard (aka production); IRgb24Destination, IBitmapDestination, IResizableDestination
	// pindmd1/2: IGray2Destination, IGray4Destination, IResizableDestination, IFixedSizeDestination
	// pin2dmd: IGray2Destination, IGray4Destination, IColoredGray2Destination, IColoredGray4Destination, IFixedSizeDestination
	// pindmd3: IGray2Destination, IGray4Destination, IColoredGray2Destination, IFixedSizeDestination
	{
		public Dimensions Size { get; private set; } = new Dimensions(128, 32);
		public bool IsAvailable { get; } = true;

		public double DotSize {
			get { return _dotSize; }
			set { _dotSize = value; UpdateEffectParams(); }
		}

		public VirtualDisplay Host { set; get; }
		public bool IgnoreAspectRatio {
			get { return Dmd.Stretch == Stretch.UniformToFill; }
			set { Dmd.Stretch = value ? Stretch.Fill : Stretch.UniformToFill; }
		}

		private double _hue;
		private double _sat;
		private double _lum;

		private double _dotSize = 1.0;
		private Color[] _gray2Palette;
		private Color[] _gray4Palette;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VirtualDmdControl()
		{
			InitializeComponent();
			ClearColor();
		}

		public void RenderBitmap(BmpFrame frame)
		{
			try {
				Dispatcher.Invoke(() => Dmd.Source = frame.Bitmap);
			} catch (TaskCanceledException e) {
				Logger.Warn(e, "Virtual DMD renderer task seems to be lost.");
			}
		}

		public void RenderGray2(DmdFrame frame)
		{
			if (_gray2Palette != null) {
				RenderRgb24(frame.ConvertToRgb24(_gray2Palette));

			} else {
				RenderBitmap(frame.ConvertFromGray2(_hue, _sat, _lum));
			}
		}

		public void RenderGray4(DmdFrame frame)
		{
			if (_gray4Palette != null) {
				RenderRgb24(frame.ConvertToRgb24(_gray4Palette));
			} else {
				RenderBitmap(frame.ConvertFromGray4(_hue, _sat, _lum));
			}
		}

		public void RenderRgb24(DmdFrame frame)
		{
			if (frame.Data.Length % 3 != 0) {
				throw new ArgumentException("RGB24 buffer must be divisible by 3, but " + frame.Data.Length + " isn't.");
			}
			RenderBitmap(frame.ConvertToBmp());
		}

		public void RenderColoredGray2(ColoredFrame frame)
		{
			SetPalette(frame.Palette);
			var coloredFrameData = FrameUtil.Join(Size, frame.Planes);
			RenderGray2(new DmdFrame(frame.Dimensions, coloredFrameData));
		}


		public void RenderColoredGray4(ColoredFrame frame)
		{
			SetPalette(frame.Palette);
			var coloredFrameData = FrameUtil.Join(Size, frame.Planes);
			RenderGray4(new DmdFrame(frame.Dimensions, coloredFrameData));
		}

		public void SetDimensions(Dimensions dimensions)
		{
			Logger.Info("Resizing virtual DMD to {0}", dimensions);
			Size = dimensions;
			UpdateEffectParams();
			Host.SetDimensions(Size);
		}

		private void UpdateEffectParams()
		{
			Dispatcher.Invoke(() => {
				DmdEffect.AspectRatio = Size.AspectRatio;
				DmdEffect.BlockCount = Math.Max(Size.Width, Size.Height);
				DmdEffect.Max = DmdEffect.AspectRatio * 0.47 * _dotSize;
			});
		}

		public void SetColor(Color color)
		{
			ColorUtil.RgbToHsl(color.R, color.G, color.B, out _hue, out _sat, out _lum);
		}

		public void SetPalette(Color[] colors, int index = -1)
		{
			_gray2Palette = ColorUtil.GetPalette(colors, 4);
			_gray4Palette = ColorUtil.GetPalette(colors, 16);
		}

		public void ClearPalette()
		{
			_gray2Palette = null;
			_gray4Palette = null;
		}

		public void ClearColor()
		{
			SetColor(RenderGraph.DefaultColor);
		}

		public void Init()
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (this is IFixedSizeDestination) {
				SetDimensions(Size);
			}
		}

		public void ClearDisplay()
		{
			RenderGray4(new DmdFrame(Size));
		}

		public void Dispose()
		{
			// nothing to dispose
		}
	}
}
