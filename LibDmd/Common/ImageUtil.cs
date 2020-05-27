using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibDmd.Frame;
using NLog;
using PixelFormat = System.Windows.Media.PixelFormat;
using Point = System.Drawing.Point;

namespace LibDmd.Common
{
	public static class ImageUtil
	{
		private static readonly Dictionary<int, FrameData> FrameDatas = new Dictionary<int, FrameData>();
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Returns a frame object for the given size.
		///
		/// The idea is not to instantiate a frame object for every frame but
		/// only for every size.
		/// </summary>
		/// <param name="dim">Frame dimensions</param>
		/// <returns></returns>
		private static FrameData GetFrameData(Dimensions dim)
		{
			var key = dim.Surface;
			if (!FrameDatas.ContainsKey(key)) {
				FrameDatas.Add(key, new FrameData());
			}
			return FrameDatas[key];
		}

		public static byte[] ConvertToGray2(BitmapSource bmp)
		{
			return ConvertToGray2(bmp, 0, 1, out _);
		}

		/// <summary>
		/// Converts a bitmap to a 2-bit grayscale array by using the luminosity of the pixels and
		/// histogram stretching.
		/// </summary>
		/// <param name="bmp">Source bitmap</param>
		/// <param name="minLum">Min threshold for luminosity histogram stretching</param>
		/// <param name="maxLum">Max threshold for luminosity histogram stretching</param>
		/// <param name="hue">Detected hue</param>
		/// <returns>Array with value for every pixel between 0 and 3</returns>
		public static byte[] ConvertToGray2(BitmapSource bmp, double minLum, double maxLum, out double hue)
		{
			var frame = new byte[bmp.PixelWidth * bmp.PixelHeight];
			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
			var bytes = new byte[bytesPerPixel];
			var rect = new Int32Rect(0, 0, 1, 1);
			double imageHue = 0;
			for (var y = 0; y < bmp.PixelHeight; y++) {
				rect.Y = y;
				for (var x = 0; x < bmp.PixelWidth; x++) {

					rect.X = x;
					bmp.CopyPixels(rect, bytes, bytesPerPixel, 0);

					// convert to HSL
					double h;
					double saturation;
					double luminosity;
					ColorUtil.RgbToHsl(bytes[2], bytes[1], bytes[0], out h, out saturation, out luminosity);

					var pixelBrightness = (luminosity - minLum) / (maxLum - minLum);
					byte frameVal = (byte)Math.Min(Math.Max(Math.Round(pixelBrightness * 3d), 0), 3);
					frame[y * bmp.PixelWidth + x] = frameVal;

					// Don't use very low luminosity values to calculate hue because they are less accurate.
					if (frameVal > 0) {
						imageHue = h;
					}
				}
			}
			hue = imageHue;
			return frame;
		}

		/// <summary>
		/// Converts an RGB24 frame to a grayscale array.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frameRgb24">RGB24 frame, top left to bottom right, three bytes per pixel with values between 0 and 255</param>
		/// <param name="numColors">Number of gray tones. 4 for 2 bit, 16 for 4 bit</param>
		/// <returns>Gray2 frame, top left to bottom right, one byte per pixel with values between 0 and 3</returns>
		public static byte[] ConvertToGray(Dimensions dim, byte[] frameRgb24, int numColors)
		{
			var frame = new byte[dim.Width * dim.Height];
			var pos = 0;
			for (var y = 0; y < dim.Height; y++) {
				for (var x = 0; x < dim.Width * 3; x += 3) {
					var rgbPos = y * dim.Width * 3 + x;

					// convert to HSL
					double hue;
					double saturation;
					double luminosity;
					ColorUtil.RgbToHsl(frameRgb24[rgbPos], frameRgb24[rgbPos + 1], frameRgb24[rgbPos + 2], out hue, out saturation, out luminosity);
					frame[pos++] = (byte)Math.Round(luminosity * (numColors - 1));
				}
			}
			return frame;
		}

		/// <summary>
		/// Converts a bitmap to a 4-bit grayscale array.
		/// </summary>
		/// <param name="bmp">Source bitmap</param>
		/// <param name="lum">Multiply luminosity</param>
		/// <returns>Array with value for every pixel between 0 and 15</returns>
		public static byte[] ConvertToGray4(BitmapSource bmp, double lum = 1)
		{
			var frame = new byte[bmp.PixelWidth * bmp.PixelHeight];
			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
			var bytes = new byte[bytesPerPixel];
			var rect = new Int32Rect(0, 0, 1, 1);
			for (var y = 0; y < bmp.PixelHeight; y++) {
				rect.Y = y;
				for (var x = 0; x < bmp.PixelWidth; x++) {

					rect.X = x;
					bmp.CopyPixels(rect, bytes, bytesPerPixel, 0);

					// convert to HSL
					double hue;
					double saturation;
					double luminosity;
					ColorUtil.RgbToHsl(bytes[2], bytes[1], bytes[0], out hue, out saturation, out luminosity);

					frame[y * bmp.PixelWidth + x] = (byte)Math.Round(luminosity * 15d * lum);
				}
			}
			return frame;
		}

		/// <summary>
		/// Converts a bitmap to a 2- or 4-bit grayscale or rgb24 array.
		/// </summary>
		/// <param name="bitLength">Bit length, 2, 4 or 24.</param>
		/// <param name="bmp">Source bitmap</param>
		/// <param name="lum">Multiply luminosity</param>
		/// <returns>Array with value for every pixel between 0 and 15</returns>
		public static byte[] ConvertTo(int bitLength, BitmapSource bmp, double lum = 1)
		{
			switch (bitLength) {
				case 2: return ConvertToGray4(bmp, lum);
				case 4: return ConvertToGray4(bmp, lum);
				case 24: return ConvertToRgb24(bmp, lum);
				default: throw new ArgumentException("Bit length must be either 2, 4 or 24.");
			}
		}

		/// <summary>
		/// Converts a bitmap to an RGB24 array.
		/// </summary>
		/// <param name="bmp">Source bitmap</param>
		/// <param name="lum">Multiply luminosity</param>
		/// <returns>Array filled with RGB values for each pixel between 0 and 255.</returns>
		public static byte[] ConvertToRgb24(BitmapSource bmp, double lum = 1)
		{
			var frame = new byte[bmp.PixelWidth * bmp.PixelHeight * 3];
			ConvertToRgb24(bmp, frame, 0, lum);
			return frame;
		}

		/// <summary>
		/// Converts a bitmap to an RGB24 array.
		/// </summary>
		/// <param name="bmp">Source bitmap</param>
		/// <param name="buffer">Destination buffer. Will be filled with RGB values for each pixel between 0 and 255.</param>
		/// <param name="offset">Offset in destination array</param>
		/// <param name="lum">Multiply luminosity</param>
		public static void ConvertToRgb24(BitmapSource bmp, byte[] buffer, int offset = 0, double lum = 1)
		{
			var stride = bmp.PixelWidth * (bmp.Format.BitsPerPixel / 8);

			var bytes = new byte[bmp.PixelHeight * stride];
			bmp.CopyPixels(bytes, stride, 0);

			if (Math.Abs(lum - 1) > 0.01) {
				for (var i = 0; i < bytes.Length; i += 3) {
					double hue, saturation, luminosity;
					byte r, g, b;
					ColorUtil.RgbToHsl(bytes[i + 2], bytes[i + 1], bytes[i], out hue, out saturation, out luminosity);
					ColorUtil.HslToRgb(hue, saturation, luminosity * lum, out r, out g, out b);
					buffer[i] = r;
					buffer[i + 1] = g;
					buffer[i + 2] = b;
				}
			} else {
				unsafe
				{
					fixed (byte* pBuffer = buffer, pBytes = bytes)
					{
						byte* pB = pBuffer, pEnd = pBytes + bytes.Length;
						for (var pByte = pBytes; pByte < pEnd; pByte += 4, pB += 3) {
							*(pB) = *(pByte + 2);
							*(pB + 1) = *(pByte + 1);
							*(pB + 2) = *(pByte);
						}
					}
				}
			}
		}

		/// <summary>
		/// Converts an image to a BitmapSouce
		/// </summary>
		/// <param name="img">Image to convert</param>
		/// <returns>Converted bitmap</returns>
		public static BitmapSource ConvertToBitmap(Image img)
		{
			var bitmap = new Bitmap(img);
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

			var bitmapSource = BitmapSource.Create(
				bitmapData.Width, bitmapData.Height, 96, 96, ConvertPixelFormat(bitmap.PixelFormat), null,
				bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

			bitmap.UnlockBits(bitmapData);
			return bitmapSource;
		}

		public static Image ConvertToImage(BitmapSource bitmapsource)
		{
			// convert image format
			var src = new FormatConvertedBitmap();
			src.BeginInit();
			src.Source = bitmapsource;
			src.DestinationFormat = PixelFormats.Bgra32;
			src.EndInit();

			// copy to bitmap
			var bitmap = new Bitmap(src.PixelWidth, src.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			src.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
			bitmap.UnlockBits(data);

			return bitmap;
		}

		public static void ConvertRgb24ToDIB(int width, int height, byte[] from, byte[] to)
		{
			var pos = 0;
			for (var y = height - 1; y >= 0; y--) {
				for (var x = 0; x < width * 3; x += 3) {
					var fromPos = width * 3 * y + x;
					to[pos] = from[fromPos];
					to[pos + 1] = from[fromPos + 1];
					to[pos + 2] = from[fromPos + 2];
					pos += 3;
				}
			}
		}

		public static void ConvertRgb24ToBgr32(Dimensions dim, byte[] from, byte[] to)
		{
			var pos = 0;
			for (var y = 0; y < dim.Height; y++) {
				for (var x = 0; x < dim.Width * 3; x += 3) {
					var fromPos = dim.Width * 3 * y + x;
					to[pos] = from[fromPos + 2];
					to[pos + 1] = from[fromPos + 1];
					to[pos + 2] = from[fromPos];
					to[pos + 3] = 0;
					pos += 4;
				}
			}
		}

		/// <summary>
		/// Converts an 2-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="dim">Frame dimensions</param>
		/// <param name="frame">2-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		private static BitmapSource ConvertFromGray2(Dimensions dim, FrameData frame, double hue, double saturation, double luminosity)
		{
			var width = dim.Width;
			var height = dim.Height;
			if (frame.Size > 0 && frame.Size != width * height) {
				throw new ArgumentException($"Must convert to {width}x{height} but frame buffer is {frame.Size} bytes");
			}

			var bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
			var bufferSize = (Math.Abs(bmp.BackBufferStride) * height + 2);
			var frameBuffer = new byte[bufferSize];

			var index = 0;
			bmp.Lock();
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {

					try {
						var pixelLum = frame.Get(y * width + x); // 0 - 3
						var lum = luminosity * pixelLum / 3;
						byte red, green, blue;
						ColorUtil.HslToRgb(hue, saturation, lum, out red, out green, out blue);

						frameBuffer[index] = blue;
						frameBuffer[index + 1] = green;
						frameBuffer[index + 2] = red;
						index += 4;

					} catch (IndexOutOfRangeException e) {
						Logger.Error(e, $"Converting {width}x{height} with {frame.Size} bytes: Trying to get pixel at position {y * width + x}");
						throw;
					}
				}
			}
			bmp.WritePixels(new Int32Rect(0, 0, width, height), frameBuffer, bmp.BackBufferStride, 0);
			bmp.Unlock();
			bmp.Freeze();
			return bmp;
		}

		/// <summary>
		/// Converts an 4-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frame">4-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		private static BitmapSource ConvertFromGray4(Dimensions dim, FrameData frame, double hue, double saturation, double luminosity)
		{
			var bmp = new WriteableBitmap(dim.Width, dim.Height, 96, 96, PixelFormats.Bgr32, null);
			var bufferSize = (Math.Abs(bmp.BackBufferStride) * dim.Height + 2);
			var frameBuffer = new byte[bufferSize];

			var index = 0;
			bmp.Lock();
			for (var y = 0; y < dim.Height; y++) {
				for (var x = 0; x < dim.Width; x++) {

					var pixelLum = frame.Get(y * dim.Width + x);
					var lum = luminosity * pixelLum / 15;
					byte red, green, blue;
					ColorUtil.HslToRgb(hue, saturation, lum, out red, out green, out blue);

					frameBuffer[index] = blue;
					frameBuffer[index + 1] = green;
					frameBuffer[index + 2] = red;
					index += 4;
				}
			}
			bmp.WritePixels(new Int32Rect(0, 0, dim.Width, dim.Height), frameBuffer, bmp.BackBufferStride, 0);
			bmp.Unlock();
			bmp.Freeze();
			return bmp;
		}

		/// <summary>
		/// Converts an RGB24 array to a bitmap.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frame">RGB values for each pixel between 0 and 255</param>
		/// <returns>Bitmap</returns>
		private static BitmapSource ConvertFromRgb24(Dimensions dim, FrameData frame)
		{
			var bmp = new WriteableBitmap(dim.Width, dim.Height, 96, 96, PixelFormats.Bgr32, null);
			var bufferSize = (Math.Abs(bmp.BackBufferStride) * dim.Height + 2);
			var frameBuffer = new byte[bufferSize];

			unsafe
			{
				fixed (byte* pFrameArray = frame.ArraySrc, pDestArray = frameBuffer)
				{
					byte* srcPtr = (frame.IsPointer) ? frame.PointerSrc : pFrameArray;
					byte* srcEnd = srcPtr + dim.Width * dim.Height * 3;
					byte* dstPtr = pDestArray;

					for (; srcPtr < srcEnd; srcPtr += 3, dstPtr += 4) {
						*dstPtr = *(srcPtr + 2);
						*(dstPtr + 1) = *(srcPtr + 1);
						*(dstPtr + 2) = *(srcPtr);
					}
				}
			}
			bmp.Lock();
			bmp.WritePixels(new Int32Rect(0, 0, dim.Width, dim.Height), frameBuffer, bmp.BackBufferStride, 0);
			bmp.Unlock();
			bmp.Freeze();
			return bmp;
		}

		/// <summary>
		/// Converts an 2-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frame">2-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		public static unsafe BitmapSource ConvertFromGray2(Dimensions dim, byte* frame, double hue, double saturation, double luminosity)
		{
			lock (FrameDatas) {
				return ConvertFromGray2(dim, GetFrameData(dim).With(frame), hue, saturation, luminosity);
			}
		}

		/// <summary>
		/// Converts an 2-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frame">2-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		public static BitmapSource ConvertFromGray2(Dimensions dim, byte[] frame, double hue, double saturation, double luminosity)
		{
			lock (FrameDatas) {
				return ConvertFromGray2(dim, GetFrameData(dim).With(frame), hue, saturation, luminosity);
			}
		}

		/// <summary>
		/// Converts an 4-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frame">4-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		public static unsafe BitmapSource ConvertFromGray4(Dimensions dim, byte* frame, double hue, double saturation, double luminosity)
		{
			lock (FrameDatas) {
				return ConvertFromGray4(dim, GetFrameData(dim).With(frame), hue, saturation, luminosity);
			}
		}

		/// <summary>
		/// Converts an 2- or 4-bit grayscale or rgb24 array to a bitmap.
		/// </summary>
		/// <param name="bitLength">Bit length, 2, 4 or 24.</param>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frame">4-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created (for gray only)</param>
		/// <param name="saturation">Saturation in which the bitmap will be created (for gray only)</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created (for gray only)</param>
		/// <returns>Bitmap</returns>
		public static BitmapSource ConvertFrom(int bitLength, Dimensions dim, byte[] frame, double hue, double saturation, double luminosity)
		{
			lock (FrameDatas) {
				switch (bitLength) {
					case 2: return ConvertFromGray2(dim, GetFrameData(dim).With(frame), hue, saturation, luminosity);
					case 4: return ConvertFromGray4(dim, GetFrameData(dim).With(frame), hue, saturation, luminosity);
					case 24: return ConvertFromRgb24(dim, GetFrameData(dim).With(frame));
					default: throw new ArgumentException("Bit length must be either 2, 4 or 24.");
				}
			}
		}

		/// <summary>
		/// Converts an 4-bit grayscale array to a bitmap.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frame">4-bit grayscale array</param>
		/// <param name="hue">Hue in which the bitmap will be created</param>
		/// <param name="saturation">Saturation in which the bitmap will be created</param>
		/// <param name="luminosity">Maximal luminosity in which the bitmap will be created</param>
		/// <returns>Bitmap</returns>
		public static BitmapSource ConvertFromGray4(Dimensions dim, byte[] frame, double hue, double saturation, double luminosity)
		{
			lock (FrameDatas) {
				return ConvertFromGray4(dim, GetFrameData(dim).With(frame), hue, saturation, luminosity);
			}
		}

		/// <summary>
		/// Converts an RGB24 array to a bitmap.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frame">RGB values for each pixel between 0 and 255</param>
		/// <returns>Bitmap</returns>
		public static unsafe BitmapSource ConvertFromRgb24(Dimensions dim, byte* frame)
		{
			lock (FrameDatas) {
				return ConvertFromRgb24(dim, GetFrameData(dim).With(frame));
			}
		}

		/// <summary>
		/// Converts an RGB24 array to a bitmap.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frame">RGB values for each pixel between 0 and 255</param>
		/// <returns>Bitmap</returns>
		public static BitmapSource ConvertFromRgb24(Dimensions dim, byte[] frame)
		{
			lock (FrameDatas) {
				return ConvertFromRgb24(dim, GetFrameData(dim).With(frame));
			}
		}

		/// <summary>
		/// Convert an RGB24 array to a RGB565 array.
		/// </summary>
		/// <param name="dim">Image dimensions</param>
		/// <param name="frameRgb24">RGB24 array, from top left to bottom right</param>
		/// <returns></returns>
		public static char[] ConvertToRgb565(Dimensions dim, byte[] frameRgb24)
		{
			var frame = new char[dim.Surface];
			var pos = 0;
			for (var y = 0; y < dim.Height; y++) {
				for (var x = 0; x < dim.Width * 3; x += 3) {
					var rgbPos = y * dim.Width * 3 + x;
					var r = frameRgb24[rgbPos];
					var g = frameRgb24[rgbPos + 1];
					var b = frameRgb24[rgbPos + 2];

					var x1 = (r & 0xF8) | (g >> 5);          // Take 5 bits of Red component and 3 bits of G component
					var x2 = ((g & 0x1C) << 3) | (b >> 3);   // Take remaining 3 Bits of G component and 5 bits of Blue component

					frame[pos++] = (char)((x1 << 8) + x2);
				}
			}
			return frame;
		}

		/// <summary>
		/// Converts between pixel formats.
		/// </summary>
		/// <param name="sourceFormat">Source format</param>
		/// <returns>Destination format</returns>
		private static PixelFormat ConvertPixelFormat(System.Drawing.Imaging.PixelFormat sourceFormat)
		{
			switch (sourceFormat) {
				case System.Drawing.Imaging.PixelFormat.Format24bppRgb: return PixelFormats.Bgr24;
				case System.Drawing.Imaging.PixelFormat.Format32bppArgb: return PixelFormats.Bgra32;
				case System.Drawing.Imaging.PixelFormat.Format32bppRgb: return PixelFormats.Bgr32;
				case System.Drawing.Imaging.PixelFormat.Indexed: return PixelFormats.Indexed1;
				case System.Drawing.Imaging.PixelFormat.Format1bppIndexed: return PixelFormats.Indexed1;
				case System.Drawing.Imaging.PixelFormat.Format4bppIndexed: return PixelFormats.Indexed4;
				case System.Drawing.Imaging.PixelFormat.Format8bppIndexed: return PixelFormats.Indexed8;
				case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale: return PixelFormats.Gray16;
				case System.Drawing.Imaging.PixelFormat.Format16bppRgb555: return PixelFormats.Bgr555;
				case System.Drawing.Imaging.PixelFormat.Format16bppRgb565: return PixelFormats.Bgr565;
				case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555: return PixelFormats.Bgr101010;
				case System.Drawing.Imaging.PixelFormat.Format32bppPArgb: return PixelFormats.Pbgra32;
				case System.Drawing.Imaging.PixelFormat.Format48bppRgb: return PixelFormats.Rgb48;
				case System.Drawing.Imaging.PixelFormat.Format64bppArgb: return PixelFormats.Rgba64;
				case System.Drawing.Imaging.PixelFormat.Format64bppPArgb: return PixelFormats.Prgba64;
				case System.Drawing.Imaging.PixelFormat.Gdi:
				case System.Drawing.Imaging.PixelFormat.Alpha:
				case System.Drawing.Imaging.PixelFormat.PAlpha:
				case System.Drawing.Imaging.PixelFormat.Extended:
				case System.Drawing.Imaging.PixelFormat.Canonical:
				case System.Drawing.Imaging.PixelFormat.Undefined:
				case System.Drawing.Imaging.PixelFormat.Max:
				default:
					return new PixelFormat();
			}
		}

		/// <summary>
		/// Sometimes we have a pointer, sometimes an array, but we don't want to implement
		/// everything twice, so this is a wrapper that supports both.
		/// </summary>
		private unsafe class FrameData
		{
			public int Size => IsPointer ? -1 : ArraySrc.Length;

			public byte* PointerSrc;
			public byte[] ArraySrc;
			public bool IsPointer;

			public FrameData With(byte* src)
			{
				PointerSrc = src;
				IsPointer = true;
				return this;
			}

			public FrameData With(byte[] src)
			{
				ArraySrc = src;
				IsPointer = false;
				return this;
			}

			public byte Get(int pos)
			{
				return IsPointer ? PointerSrc[pos] : ArraySrc[pos];
			}
		}
	}
}
