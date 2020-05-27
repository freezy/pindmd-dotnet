using System;
using System.Windows.Media;
using LibDmd.Common;
using LibDmd.Output;
using NLog;

namespace LibDmd.Frame
{
	/// <summary>
	/// A frame sent without a palette or color.
	/// </summary>RGB24 buffer must be divisible by 3
	public class DmdFrame : BaseFrame, ICloneable
	{
		public byte[] Data { get; private set; }

		public DmdFrame()
		{
		}

		public DmdFrame(Dimensions dim)
		{
			Dimensions = dim;
			Data = new byte[dim.Surface];
		}

		public DmdFrame(Dimensions dim, byte[] data)
		{
			Dimensions = dim;
			Data = data;
		}

		public DmdFrame Update(DmdFrame frame)
		{
			Dimensions = frame.Dimensions;
			Data = frame.Data;
			return this;
		}

		public DmdFrame Update(byte[] data)
		{
			Data = data;
			return this;
		}

		public DmdFrame Update(Dimensions dim)
		{
			Dimensions = dim;
			return this;
		}

		public DmdFrame Update(Dimensions dim, byte[] data)
		{
			Dimensions = dim;
			Data = data;
			return this;
		}

		public DmdFrame ConvertToRgb24(Color[] palette)
		{
			Data = ColorUtil.ColorizeFrame(Dimensions, Data, palette);
			return this;
		}

		public DmdFrame ConvertGrayToGray(params byte[] mapping)
		{
			Data = FrameUtil.ConvertGrayToGray(Data, mapping);
			return this;
		}

		public DmdFrame ConvertToGray(int numColors)
		{
			Data = ImageUtil.ConvertToGray(Dimensions, Data, numColors);
			return this;
		}

		public BmpFrame ConvertToBmp()
		{
			return new BmpFrame(ImageUtil.ConvertFromRgb24(Dimensions, Data));
		}

		public BmpFrame ConvertFromGray2(double hue, double sat, double lum)
		{
			return new BmpFrame(ImageUtil.ConvertFromGray2(Dimensions, Data, hue, sat, lum));
		}

		public BmpFrame ConvertFromGray4(double hue, double sat, double lum)
		{
			return new BmpFrame(ImageUtil.ConvertFromGray4(Dimensions, Data, hue, sat, lum));
		}

		public BmpFrame ConvertFromRgb24(Color[] palette)
		{
			return new BmpFrame(ImageUtil.ConvertFromRgb24(
				Dimensions,
				ConvertToRgb24(palette).Data
			));
		}

		public DmdFrame TransformGray2(RenderGraph renderGraph, IFixedSizeDestination fixedDest, IMultiSizeDestination multiDest)
		{
			return Transform(2, 1, renderGraph, fixedDest, multiDest);
		}

		public DmdFrame TransformGray4(RenderGraph renderGraph, IFixedSizeDestination fixedDest, IMultiSizeDestination multiDest)
		{
			return Transform(4, 1, renderGraph, fixedDest, multiDest);
		}

		public DmdFrame TransformRgb24(RenderGraph renderGraph, IFixedSizeDestination fixedDest, IMultiSizeDestination multiDest)
		{
			return Transform(24, 3, renderGraph, fixedDest, multiDest);
		}

		/// <summary>
		/// Up- or downscales image, and flips if necessary.
		/// </summary>
		///
		/// <remarks>
		/// Images are actually never upscaled, if the source image is smaller than destination frame, it gets centered.
		/// Downscaling is done depending on the render graph's `Resize` setting.
		/// </remarks>
		/// <param name="bitLen">Number of bit planes</param>
		/// <param name="bytesPerPixel">Pixel size in bytes</param>
		/// <param name="renderGraph">Render graph reference to retrieve flipping and resizing config</param>
		/// <param name="fixedDest">If not null, the fixed destination we're transforming for.</param>
		/// <param name="multiDest">If not null, the multi-res destination we're transforming for.</param>
		/// <returns></returns>
		private DmdFrame Transform(int bitLen, int bytesPerPixel, RenderGraph renderGraph, IFixedSizeDestination fixedDest, IMultiSizeDestination multiDest)
		{
			var targetDim = GetTargetDimensions(fixedDest, multiDest);
			if (targetDim == Dimensions.Dynamic || Dimensions == targetDim) {
				// no resizing, we're done here.
				return Flip(bytesPerPixel, renderGraph.FlipHorizontally, renderGraph.FlipVertically);
			}

			// perf: if no flipping these cases can easily done on the byte array directly
			if (!renderGraph.FlipHorizontally && !renderGraph.FlipVertically) {

				// copy whole block if only vertical padding
				if (Dimensions.Width == targetDim.Width && Dimensions.Height < targetDim.Height) {
					return Update(targetDim, CenterVertically(targetDim, Data, bytesPerPixel));
				}

				// copy line by line if centering image
				if (Dimensions < targetDim) {
					return Update(targetDim, CenterFrame(targetDim, Data, bytesPerPixel));
				}
			}

			// otherwise, convert to bitmap, resize, convert back.
			var bmp = ImageUtil.ConvertFrom(bitLen, Dimensions, Data, 0, 1, 1);
			var transformedBmp = TransformationUtil.Transform(bmp, targetDim, renderGraph.Resize, renderGraph.FlipHorizontally, renderGraph.FlipVertically);
			var transformedFrame = ImageUtil.ConvertTo(bitLen, transformedBmp);

			return Update(targetDim, transformedFrame);
		}

		private DmdFrame Flip(int bytesPerPixel, bool flipHorizontally, bool flipVertically)
		{
			Data = TransformationUtil.Flip(Dimensions, bytesPerPixel, Data, flipHorizontally, flipVertically);
			return this;
		}

		/// <summary>
		/// Flat-clones the frame (i.e. the data is still the same, but now you
		/// can replace it without affecting other references of the frame).
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			return new DmdFrame(Dimensions, Data);
		}
	}
}
