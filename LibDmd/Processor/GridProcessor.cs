using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LibDmd.Processor
{
	/// <summary>
	/// A processor that strips off the space between the "dot grid" of
	/// a rendered DMD frame.
	/// </summary>
	public class GridProcessor : AbstractProcessor
	{
		public override bool Enabled
		{
			get { return _enabled && (Spacing > 0 || CropTop > 0 || CropLeft > 0 || CropRight > 0 || CropBottom > 0); }
			set { _enabled = value; }
		}

		/// <summary>
		/// Number of horizontal dots
		/// </summary>
		public int Width { get; set; } = 128;

		/// <summary>
		/// Number of vertical dots
		/// </summary>
		public int Height { get; set; } = 32;

		/// <summary>
		/// Relative spacing to strip. 1 = 100% (same size as dot), 0.5 = half size, etc
		/// </summary>
		public double Spacing { get; set; }

		public int CropLeft { get; set; } = 0;
		public int CropTop { get; set; } = 0;
		public int CropRight { get; set; } = 0;
		public int CropBottom { get; set; } = 0;

		private bool _enabled = true;

		public override BitmapSource Process(BitmapSource bmp)
		{
			var sw = new Stopwatch();
			sw.Start();

			var sliceWidth = bmp.Width / (Width + Width * Spacing - Spacing);
			var sliceHeight = bmp.Height / (Height + Height * Spacing - Spacing);
			var paddingWidth = (bmp.Width * Spacing) / (Width + Width * Spacing - Spacing);
			var paddingHeight = (bmp.Height * Spacing) / (Height + Height * Spacing - Spacing);
			var destRect = new Int32Rect();
			var srcRect = new Int32Rect();
			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;

			var wBmp = new WriteableBitmap(bmp);

			srcRect.Y = 0;
			srcRect.Width = (int)Math.Ceiling(sliceWidth);
			srcRect.Height = bmp.PixelHeight;
			destRect.Y = 0;
			destRect.Width = srcRect.Width;
			destRect.Height = bmp.PixelHeight;
			var blockSize = bytesPerPixel * srcRect.Width * bmp.PixelHeight;
			var stride = srcRect.Width * bytesPerPixel;
			var buffer = new byte[blockSize];
			for (var x = 0; x < Width; x++) {
				srcRect.X = (int)(x * (sliceWidth + paddingWidth));
				destRect.X = (int)(x * sliceWidth);
				bmp.CopyPixels(srcRect, buffer, stride, 0);
				wBmp.WritePixels(destRect, buffer, stride, 0);
			}

			srcRect.X = 0;
			srcRect.Width = bmp.PixelWidth;
			srcRect.Height = (int)Math.Ceiling(sliceHeight);
			destRect.X = 0;
			destRect.Width = bmp.PixelWidth;
			destRect.Height = srcRect.Height;
			blockSize = bytesPerPixel * bmp.PixelWidth * srcRect.Height;
			stride = bmp.PixelWidth * bytesPerPixel;
			buffer = new byte[blockSize];
			for (var y = 0; y < Height; y++) {
				srcRect.Y = (int)(y * (sliceHeight + paddingHeight));
				destRect.Y = (int)(y * sliceHeight);
				wBmp.CopyPixels(srcRect, buffer, stride, 0);
				wBmp.WritePixels(destRect, buffer, stride, 0);
			}
			var img = new CroppedBitmap(wBmp, new Int32Rect(
				CropLeft, 
				CropTop, 
				(int)(sliceWidth * Width) - CropLeft - CropRight, 
				(int)(sliceHeight * Height) - CropTop - CropBottom));

			sw.Stop();
			//Console.WriteLine("Grid-sized from {0}x{1} to {2}x{3} in {4}ms.", bmp.Width, bmp.Height, img.PixelWidth, img.PixelHeight, sw.ElapsedMilliseconds);

			img.Freeze();
			_whenProcessed.OnNext(img);
			return img;
		}
	}
}
