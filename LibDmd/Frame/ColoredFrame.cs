using System;
using System.Windows.Media;
using LibDmd.Common;
using LibDmd.Output;
using NLog;

namespace LibDmd.Frame
{
	public class ColoredFrame : BaseFrame, ICloneable
	{
		/// <summary>
		/// Frame data, split into bit planes
		/// </summary>
		public byte[][] Planes { get; private set; }

		/// <summary>
		/// Color palette
		/// </summary>
		public Color[] Palette { get; private set; }

		/// <summary>
		/// Palette index from animation or -1 if not set.
		/// </summary>
		public int PaletteIndex { get; }

		public ColoredFrame()
		{
		}

		public ColoredFrame(Dimensions dim, byte[][] planes, Color[] palette, int paletteIndex)
		{
			Dimensions = dim;
			Planes = planes;
			Palette = palette;
			PaletteIndex = paletteIndex;
		}

		public ColoredFrame(Dimensions dim, byte[][] planes, Color[] palette)
		{
			Dimensions = dim;
			Planes = planes;
			Palette = palette;
			PaletteIndex = -1;
		}

		public ColoredFrame(Dimensions dim, byte[] frame, Color color)
		{
			Planes = FrameUtil.Split(dim, 2, frame);
			Palette = ColorUtil.GetPalette(new[] { Colors.Black, color }, 4);
		}

		public ColoredFrame Update(Dimensions dimensions)
		{
			Dimensions = dimensions;
			return this;
		}

		public ColoredFrame Update(Dimensions dimensions, byte[][] planes)
		{
			Dimensions = dimensions;
			Planes = planes;
			return this;
		}

		public ColoredFrame Update(byte[][] planes)
		{
			Planes = planes;
			return this;
		}

		public ColoredFrame Update(byte[][] planes, Color[] palette)
		{
			Planes = planes;
			Palette = palette;
			return this;
		}

		public ColoredFrame Flip(bool flipHorizontally, bool flipVertically)
		{
			Planes = TransformationUtil.Flip(Dimensions, Planes, flipHorizontally, flipVertically);
			return this;
		}

		public DmdFrame ConvertToGray(params byte[] mapping)
		{
			var data = FrameUtil.Join(Dimensions, Planes);
			return new DmdFrame(Dimensions, FrameUtil.ConvertGrayToGray(data, mapping));
		}

		public DmdFrame ConvertToGray()
		{
			return new DmdFrame(Dimensions, FrameUtil.Join(Dimensions, Planes));
		}

		public DmdFrame ConvertToRgb24()
		{
			return new DmdFrame(Dimensions, ColorUtil.ColorizeFrame(
				Dimensions,
				FrameUtil.Join(Dimensions, Planes),
				Palette
			));
		}

		public ColoredFrame TransformColoredGray2(RenderGraph renderGraph, IFixedSizeDestination fixedDest, IMultiSizeDestination multiDest)
		{
			return TransformColoredGray(2, renderGraph, fixedDest, multiDest);
		}

		public ColoredFrame TransformColoredGray4(RenderGraph renderGraph, IFixedSizeDestination fixedDest, IMultiSizeDestination multiDest)
		{
			return TransformColoredGray(4, renderGraph, fixedDest, multiDest);
		}

		private ColoredFrame TransformColoredGray(int bitLen, RenderGraph renderGraph, IFixedSizeDestination fixedDest, IMultiSizeDestination multiDest)
		{
			var targetDim = GetTargetDimensions(fixedDest, multiDest);
			if (targetDim == Dimensions.Dynamic || Dimensions == targetDim) {
				// no resizing, we're done here.
				return Flip(renderGraph.FlipHorizontally, renderGraph.FlipVertically);
			}

			// perf: if no flipping these cases can easily done on the byte array directly
			if (!renderGraph.FlipHorizontally && !renderGraph.FlipVertically) {

				// copy whole block if only vertical padding
				if (Dimensions.Width == targetDim.Width && Dimensions.Height < targetDim.Height) {
					var centeredData = CenterVertically(targetDim, FrameUtil.Join(Dimensions, Planes), 1);
					return Update(targetDim, FrameUtil.Split(targetDim, bitLen, centeredData));
				}

				// copy line by line if centering image
				if (Dimensions < targetDim) {
					var centeredData = CenterFrame(targetDim, FrameUtil.Join(Dimensions, Planes), 1);
					return Update(targetDim, FrameUtil.Split(targetDim, bitLen, centeredData));
				}
			}

			// otherwise, convert to bitmap, resize, convert back.
			var bmp = ImageUtil.ConvertFrom(bitLen, Dimensions, FrameUtil.Join(Dimensions, Planes), 0, 1, 1);
			var transformedBmp = TransformationUtil.Transform(bmp, targetDim, renderGraph.Resize, renderGraph.FlipHorizontally, renderGraph.FlipVertically);
			var transformedFrame = ImageUtil.ConvertTo(bitLen, transformedBmp);

			return Update(targetDim, FrameUtil.Split(targetDim, bitLen, transformedFrame));
		}

		public BmpFrame ConvertToBmp()
		{
			return new BmpFrame(
				ImageUtil.ConvertFromRgb24(Dimensions,
					ColorUtil.ColorizeFrame(
						Dimensions,
						FrameUtil.Join(Dimensions, Planes),
						Palette
					)
				)
			);
		}

		public object Clone()
		{
			return new ColoredFrame(Dimensions, Planes, Palette, PaletteIndex);
		}

	}
}
