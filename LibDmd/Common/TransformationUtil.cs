using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibDmd.Frame;
using ResizeMode = LibDmd.Input.ResizeMode;

namespace LibDmd.Common
{
	public class TransformationUtil
	{
		/// <summary>
		/// Flips a top-left to bottom-right array of pixels with a given number of bytes per pixel.
		/// </summary>
		/// <param name="dim">Pixel dimensions</param>
		/// <param name="bytesPerPixel">How many bytes per pixel</param>
		/// <param name="frame">Pixel data</param>
		/// <param name="flipHorizontally">If true, flip horizontally (left/right)</param>
		/// <param name="flipVertically">If true, flip vertically (top/down)</param>
		/// <returns></returns>
		public static byte[] Flip(Dimensions dim, int bytesPerPixel, byte[] frame, bool flipHorizontally, bool flipVertically)
		{
			if (!flipHorizontally && !flipVertically) {
				return frame;
			}
			var pos = 0;
			var flipped = new byte[frame.Length];
			for (var y = 0; y < dim.Height; y++) {
				for (var x = 0; x < dim.Width * bytesPerPixel; x += bytesPerPixel) {
					var xFlipped = flipHorizontally ? (dim.Width - 1) * bytesPerPixel - x : x;
					var yFlipped = flipVertically ? dim.Height - y - 1 : y;
					for (var v = 0; v < bytesPerPixel; v++) {
						flipped[pos + v] = frame[dim.Width * bytesPerPixel * yFlipped + xFlipped + v];
					}
					pos += bytesPerPixel;
				}
			}
			return flipped;
		}

		/// <summary>
		/// Flips a given number of bit planes.
		/// </summary>
		/// <param name="dim">Pixel dimensions</param>
		/// <param name="planes">Bit planes</param>
		/// <param name="flipHorizontally">If true, flip horizontally (left/right)</param>
		/// <param name="flipVertically">If true, flip vertically (top/down)</param>
		/// <returns></returns>
		public static byte[][] Flip(Dimensions dim, byte[][] planes, bool flipHorizontally, bool flipVertically)
		{
			if (!flipHorizontally && !flipVertically) {
				return planes;
			}
			var flippedPlanes = new byte[planes.Length][];
			for (var n = 0; n < planes.Length; n++) {
				var pos = 0;
				var plane = new BitArray(planes[n]);
				var flippedPlane = new BitArray(plane.Length);
				for (var y = 0; y < dim.Height; y++) {
					for (var x = 0; x < dim.Width; x ++) {
						var xFlipped = flipHorizontally ? (dim.Width - 1) - x : x;
						var yFlipped = flipVertically ? dim.Height - y - 1 : y;
						flippedPlane.Set(pos, plane[dim.Width * yFlipped + xFlipped]);
						pos++;
					}
				}
				var fp = new byte[planes[n].Length];
				flippedPlane.CopyTo(fp, 0);
				flippedPlanes[n] = fp;
			}
			return flippedPlanes;
		}

		/// <summary>
		/// Resizes and flips an image
		/// </summary>
		/// <param name="bmp">Source image</param>
		/// <param name="destDim">Resize to these dimensions</param>
		/// <param name="resize">How to scale down</param>
		/// <param name="flipHorizontally">If true, flip horizontally (left/right)</param>
		/// <param name="flipVertically">If true, flip vertically (top/down)</param>
		/// <returns>New transformed image or the same image if new dimensions are identical and no flipping taking place</returns>
		public static BitmapSource Transform(BitmapSource bmp, Dimensions destDim, ResizeMode resize, bool flipHorizontally, bool flipVertically)
		{
			var destWidth = destDim.Width;
			var destHeight = destDim.Height;
			if (bmp.PixelWidth == destWidth && bmp.PixelHeight == destHeight && !flipHorizontally && !flipVertically) {
				return bmp;
			}

			var sw = new Stopwatch();
			sw.Start();

			var srcAr = (double)bmp.PixelWidth / bmp.PixelHeight;
			var destAr = (double)destWidth / destHeight;
			var sameAr = Math.Abs(destAr - srcAr) < 0.01;

			double width;
			double height;
			var marginX = 0;
			var marginY = 0;
			var cropX = 0;
			var cropY = 0;

			// image fits into dest, don't upscale, just adjust margins.
			if (destWidth > bmp.PixelWidth && destHeight > bmp.PixelHeight) {
				marginX = (destWidth - bmp.PixelWidth) / 2;
				marginY = (destHeight - bmp.PixelHeight) / 2;
				width = bmp.PixelWidth;
				height = bmp.PixelHeight;

			// width fits into dest, only scale y-axis
			} else if (destWidth > bmp.PixelWidth) {
				marginX = (destWidth - bmp.PixelWidth) / 2;
				width = bmp.PixelWidth;
				switch (resize) {
					case ResizeMode.Stretch:
						height = destHeight;
						break;
					case ResizeMode.Fill:
						height = bmp.PixelHeight;
						cropY = (int)((height - destHeight) / 2);
						break;
					case ResizeMode.Fit:
						height = destHeight;
						width = destHeight * srcAr;
						marginX = (int)Math.Round((destWidth - width) / 2);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(resize), resize, null);
				}

			// height fits into dest, only scale x-axis
			} else if (destHeight > bmp.PixelHeight) {
				marginY = (destHeight - bmp.PixelHeight) / 2;
				height = bmp.PixelHeight;
				switch (resize) {
					case ResizeMode.Stretch:
						width = destWidth;
						break;
					case ResizeMode.Fill:
						width = bmp.PixelWidth;
						cropX = (int)((width - destWidth) / 2);
						break;
					case ResizeMode.Fit:
						width = destWidth;
						height = destWidth / srcAr;
						marginY = (int)Math.Round((destHeight - height) / 2);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(resize), resize, null);
				}

			// now the most common case: do nothing.
			} else if (destWidth == bmp.PixelWidth && destHeight == bmp.PixelHeight) {
				width = bmp.PixelWidth;
				height = bmp.PixelHeight;

			// downscale: resize to fill
			} else if (!sameAr && resize == ResizeMode.Fill) {
				if (destAr > srcAr) {
					width = destWidth;
					height = destWidth / srcAr;
					cropY = (int)((height - destHeight) / 2);
				} else {
					width = destHeight * srcAr;
					height = destHeight;
					cropX = (int)((width - destWidth) / 2);
				}

			// downscale: resize to fit
			} else if (!sameAr && resize == ResizeMode.Fit) {
				if (destAr > srcAr) {
					width = destHeight * srcAr;
					height = destHeight;
					marginX = (int)Math.Round((destWidth - width) / 2);
				} else {
					width = destWidth;
					height = destWidth / srcAr;
					marginY = (int)Math.Round((destHeight - height) / 2);
				}

			// otherwise, stretch.
			} else {
				width = destWidth;
				height = destHeight;
			}
			//Console.WriteLine("[{6}]: size: {0}x{1}, crop: {2}/{3}, margins: {4}/{5}", width, height, cropX, cropY, marginX, marginY, resize);

			BitmapSource processedBmp;
			if (bmp.PixelWidth == (int)width && bmp.PixelHeight == (int)height && !flipHorizontally && !flipVertically) {
				processedBmp = bmp;
			} else {
				processedBmp = new TransformedBitmap(bmp, new ScaleTransform(width / bmp.PixelWidth * (flipHorizontally ? -1 : 1), height / bmp.PixelHeight * (flipVertically ? -1 : 1), (double) bmp.PixelWidth/2, (double) bmp.PixelHeight/2));
			}

			// crop if necessary
			if (cropX > 0 || cropY > 0) {
				var cropParams = new Int32Rect(cropX, cropY, Math.Min(destWidth, processedBmp.PixelWidth), Math.Min(destHeight, processedBmp.PixelHeight));
				processedBmp = new CroppedBitmap(processedBmp, cropParams);
				//Console.WriteLine("Cropped bitmap: {0}x{1}", processedBmp.PixelWidth, processedBmp.PixelHeight);
			}

			// fit needs painting on new canvas
			if (marginX > 0 || marginY > 0) {
				var bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
				var blockSize = bytesPerPixel * processedBmp.PixelWidth * processedBmp.PixelHeight;
				var buffer = new byte[blockSize];
				var stride = processedBmp.PixelWidth * bytesPerPixel;

				// create new canvas
				var emptyBmp = new WriteableBitmap(destWidth, destHeight, 96, 96, PixelFormats.Bgr32, bmp.Palette);

				// copy resized bitmap to new canvas
				var rect = new Int32Rect(0, 0, processedBmp.PixelWidth, processedBmp.PixelHeight);
				processedBmp.CopyPixels(rect, buffer, stride, 0);
				rect.X = marginX;
				rect.Y = marginY;
				emptyBmp.WritePixels(rect, buffer, stride, 0);
				processedBmp = emptyBmp;
				//Console.WriteLine("Repainted bitmap: {0}x{1}", processedBmp.PixelWidth, processedBmp.PixelHeight);
			}

			processedBmp.Freeze();
			return processedBmp;
		}
	}
}
