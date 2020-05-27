using System;
using System.Windows.Media.Imaging;
using LibDmd.Common;
using LibDmd.Input;
using LibDmd.Output;

namespace LibDmd.Frame
{
	public class BmpFrame : BaseFrame, ICloneable
	{
		public BitmapSource Bitmap;

		public BmpFrame()
		{
		}

		public BmpFrame(BitmapSource bitmap)
		{
			Bitmap = bitmap;
			Dimensions = new Dimensions(bitmap.PixelWidth, bitmap.PixelHeight);
		}

		public BmpFrame(BitmapSource bitmap, Dimensions dim)
		{
			Bitmap = bitmap;
			Dimensions = dim;
		}

		public BmpFrame Update(BitmapSource bmp)
		{
			Bitmap = bmp;
			return this;
		}

		public DmdFrame ConvertToGray2()
		{
			return new DmdFrame(Dimensions, ImageUtil.ConvertToGray2(Bitmap));
		}

		public DmdFrame ConvertToGray4()
		{
			return new DmdFrame(Dimensions, ImageUtil.ConvertToGray4(Bitmap));
		}

		public DmdFrame ConvertToRgb24()
		{
			return new DmdFrame(Dimensions, ImageUtil.ConvertToRgb24(Bitmap));
		}

		public BmpFrame Transform(RenderGraph renderGraph, IFixedSizeDestination fixedDest, IMultiSizeDestination multiDest)
		{
			var targetDim = GetTargetDimensions(fixedDest, multiDest);
			var mustResize = targetDim != Dimensions.Dynamic && Dimensions != targetDim;
			if (!mustResize && !renderGraph.FlipHorizontally && !renderGraph.FlipVertically) {
				return this;
			}
			return Transform(targetDim, renderGraph.Resize, renderGraph.FlipHorizontally, renderGraph.FlipVertically);
		}

		private BmpFrame Transform(Dimensions dim, ResizeMode resize, bool flipHorizontally, bool flipVertically)
		{
			Bitmap = TransformationUtil.Transform(Bitmap, dim, resize, flipHorizontally, flipVertically);
			return this;
		}

		public object Clone()
		{
			return new BmpFrame(Bitmap, Dimensions);
		}
	}
}
