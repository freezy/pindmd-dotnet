using System;
using System.Windows;
using System.Windows.Media.Imaging;
using LibDmd.Common;

namespace LibDmd.Output
{
	/// <summary>
	/// The base class for output renderers that fill a buffer with bitmap data
	/// in given format.
	/// </summary>
	public abstract class BufferRenderer
	{
		public bool IsAvailable { get; protected set; }

		public abstract int Width { get; set; }
		public abstract int Height { get; set; }

		/// <summary>
		/// Copies a bitmap into a 2-bit grayscale buffer. Used by PinDMDv1.
		/// </summary>
		/// <param name="bmp">Image source</param>
		/// <param name="frameBuffer">Destination buffer</param>
		/// <param name="startAt">Position to start writing</param>
		[Obsolete("Use FrameUtil instead", true)]
		protected void RenderGray2(BitmapSource bmp, byte[] frameBuffer, int startAt)
		{
			// make sure we can render
			AssertRenderReady(bmp);

			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
			var bytes = new byte[bytesPerPixel];
			var rect = new Int32Rect(0, 0, 1, 1);
			var byteIdx = startAt;

			for (var y = 0; y < Height; y++) {
				for (var x = 0; x < Width; x += 8) {
					byte bd0 = 0;
					byte bd1 = 0;

					for (var v = 7; v >= 0; v--) {

						rect.X = x + v;
						rect.Y = y;
						bmp.CopyPixels(rect, bytes, bytesPerPixel, 0);

						// convert to HSL
						double hue;
						double saturation;
						double luminosity;
						ColorUtil.RgbToHsl(bytes[2], bytes[1], bytes[0], out hue, out saturation, out luminosity);

						// pixel shade between 0 and 3
						var pixel = (byte)(luminosity * 4);
						pixel = pixel == 4 ? (byte)3 : pixel; // special case lum == 1 and hence pixel = 4

						bd0 <<= 1;
						bd1 <<= 1;

						if ((pixel & 1) != 0) {
							bd0 |= 1;
						}

						if ((pixel & 2) != 0) {
							bd1 |= 1;
						}
					}

					frameBuffer[byteIdx + 0] = bd0;
					frameBuffer[byteIdx + 512] = bd1;
					byteIdx++;
				}
			}
		}

		/// <summary>
		/// Copies a bitmap into a 4-bit grayscale buffer. Used by PinDMDv2 and PIN2DMD.
		/// </summary>
		/// <param name="bmp">Image source</param>
		/// <param name="frameBuffer">Destination buffer</param>
		/// <param name="startAt">Position to start writing</param>
		protected void RenderGray4(BitmapSource bmp, byte[] frameBuffer, int startAt)
		{
			// make sure we can render
			AssertRenderReady(bmp);

			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
			var bytes = new byte[bytesPerPixel];
			var rect = new Int32Rect(0, 0, 1, 1);
			var byteIdx = startAt;

			for (var y = 0; y < Height; y++) {
				for (var x = 0; x < Width; x += 8) {
					byte bd0 = 0;
					byte bd1 = 0;
					byte bd2 = 0;
					byte bd3 = 0;
					for (var v = 7; v >= 0; v--) {

						rect.X = x + v;
						rect.Y = y;
						bmp.CopyPixels(rect, bytes, bytesPerPixel, 0);

						// convert to HSL
						double hue;
						double saturation;
						double luminosity;
						ColorUtil.RgbToHsl(bytes[2], bytes[1], bytes[0], out hue, out saturation, out luminosity);

						var pixel = (byte)(luminosity * 255d);

						bd0 <<= 1;
						bd1 <<= 1;
						bd2 <<= 1;
						bd3 <<= 1;

						if ((pixel & 16) != 0) {
							bd0 |= 1;
						}
						if ((pixel & 32) != 0) {
							bd1 |= 1;
						}
						if ((pixel & 64) != 0) {
							bd2 |= 1;
						}
						if ((pixel & 128) != 0) {
							bd3 |= 1;
						}
					}
					frameBuffer[byteIdx] = bd0;
					frameBuffer[byteIdx + 512] = bd1;
					frameBuffer[byteIdx + 1024] = bd2;
					frameBuffer[byteIdx + 1536] = bd3;
					byteIdx++;
				}
			}
		}

		/// <summary>
		/// Copies byte array into a 4-bit grayscale buffer. Used by PinDMDv2 and PIN2DMD.
		/// </summary>
		/// <param name="frame">Array containing Width * Height bytes, with values between 0 and 15 for every pixel.</param>
		/// <param name="frameBuffer">Destination buffer</param>
		/// <param name="startAt">Position to start writing</param>
		protected void RenderGray4(byte[] frame, byte[] frameBuffer, int startAt)
		{
			// make sure we can render
			AssertRenderReady(frame);

			var byteIdx = startAt;

			for (var y = 0; y < Height; y++) {
				for (var x = 0; x < Width; x += 8) {
					byte bd0 = 0;
					byte bd1 = 0;
					byte bd2 = 0;
					byte bd3 = 0;
					for (var v = 7; v >= 0; v--)
					{
						var pixel = frame[y * Width + x + v]; // between 0 and 15

						bd0 <<= 1;
						bd1 <<= 1;
						bd2 <<= 1;
						bd3 <<= 1;

						if ((pixel & 1) != 0) {
							bd0 |= 1;
						}
						if ((pixel & 2) != 0) {
							bd1 |= 1;
						}
						if ((pixel & 4) != 0) {
							bd2 |= 1;
						}
						if ((pixel & 8) != 0) {
							bd3 |= 1;
						}
					}
					frameBuffer[byteIdx] = bd0;
					frameBuffer[byteIdx + 512] = bd1;
					frameBuffer[byteIdx + 1024] = bd2;
					frameBuffer[byteIdx + 1536] = bd3;
					byteIdx++;
				}
			}
		}


		/// <summary>
		/// Copies a bitmap into a 24-bit RGB buffer. Used by PIN2DMD.
		/// </summary>
		/// <param name="bmp">Image source</param>
		/// <param name="frameBuffer">Destination buffer</param>
		/// <param name="startAt">Position to start writing</param>
		protected void RenderRgb24(BitmapSource bmp, byte[] frameBuffer, int startAt)
		{
			// make sure we can render
			AssertRenderReady(bmp);

			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
			var bytes = new byte[bytesPerPixel];
			var rect = new Int32Rect(0, 0, 1, 1);
			var byteIdx = startAt;

			for (var y = 0; y < Height; y++) {
				for (var x = 0; x < Width; x += 8) {
					byte r3 = 0;
					byte r4 = 0;
					byte r5 = 0;
					byte r6 = 0;
					byte r7 = 0;

					byte g3 = 0;
					byte g4 = 0;
					byte g5 = 0;
					byte g6 = 0;
					byte g7 = 0;

					byte b3 = 0;
					byte b4 = 0;
					byte b5 = 0;
					byte b6 = 0;
					byte b7 = 0;
					for (var v = 7; v >= 0; v--) {
						rect.X = x + v;
						rect.Y = y;
						bmp.CopyPixels(rect, bytes, bytesPerPixel, 0);

						// convert to HSL
						var pixelr = bytes[2];
						var pixelg = bytes[1];
						var pixelb = bytes[0];

						r3 <<= 1;
						r4 <<= 1;
						r5 <<= 1;
						r6 <<= 1;
						r7 <<= 1;
						g3 <<= 1;
						g4 <<= 1;
						g5 <<= 1;
						g6 <<= 1;
						g7 <<= 1;
						b3 <<= 1;
						b4 <<= 1;
						b5 <<= 1;
						b6 <<= 1;
						b7 <<= 1;

						if ((pixelr & 8) != 0) r3 |= 1;
						if ((pixelr & 16) != 0) r4 |= 1;
						if ((pixelr & 32) != 0) r5 |= 1;
						if ((pixelr & 64) != 0) r6 |= 1;
						if ((pixelr & 128) != 0) r7 |= 1;

						if ((pixelg & 8) != 0) g3 |= 1;
						if ((pixelg & 16) != 0) g4 |= 1;
						if ((pixelg & 32) != 0) g5 |= 1;
						if ((pixelg & 64) != 0) g6 |= 1;
						if ((pixelg & 128) != 0) g7 |= 1;

						if ((pixelb & 8) != 0) b3 |= 1;
						if ((pixelb & 16) != 0) b4 |= 1;
						if ((pixelb & 32) != 0) b5 |= 1;
						if ((pixelb & 64) != 0) b6 |= 1;
						if ((pixelb & 128) != 0) b7 |= 1;
					}

					frameBuffer[byteIdx + 5120] = r3;
					frameBuffer[byteIdx + 5632] = r4;
					frameBuffer[byteIdx + 6144] = r5;
					frameBuffer[byteIdx + 6656] = r6;
					frameBuffer[byteIdx + 7168] = r7;

					frameBuffer[byteIdx + 2560] = g3;
					frameBuffer[byteIdx + 3072] = g4;
					frameBuffer[byteIdx + 3584] = g5;
					frameBuffer[byteIdx + 4096] = g6;
					frameBuffer[byteIdx + 4608] = g7;

					frameBuffer[byteIdx + 0] = b3;
					frameBuffer[byteIdx + 512] = b4;
					frameBuffer[byteIdx + 1024] = b5;
					frameBuffer[byteIdx + 1536] = b6;
					frameBuffer[byteIdx + 2048] = b7;
					byteIdx++;
				}
			}
		}
	
		/// <summary>
		/// Makes sure the device is available and the source has the same dimensions as the display.
		/// </summary>
		/// <param name="bmp">Bitmap</param>
		protected void AssertRenderReady(BitmapSource bmp)
		{
			if (!IsAvailable) {
				throw new SourceNotAvailableException();
			}
			if (bmp.PixelWidth != Width || bmp.PixelHeight != Height) {
				throw new Exception($"Image must have the same dimensions as the display ({Width}x{Height}).");
			}
		}

		/// <summary>
		/// Makes sure the device is available and the source has the same dimensions as the display.
		/// </summary>
		/// <param name="frame">Pixel array</param>
		/// <param name="bytesPerPixel">Bytes per pixel</param>
		protected void AssertRenderReady(byte[] frame, int bytesPerPixel = 1)
		{
			if (!IsAvailable) {
				throw new SourceNotAvailableException();
			}
			if (frame.Length != Width * Height * bytesPerPixel) {
				throw new Exception($"Image must have the same dimensions as the display ({Width}x{Height}).");
			}
		}
	}
}
