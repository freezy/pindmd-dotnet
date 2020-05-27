using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibDmd.Common;
using LibDmd.Frame;
using LibDmd.Input.ScreenGrabber;
using LibDmd.Processor;
using NLog;
using Color = System.Windows.Media.Color;

namespace LibDmd.Input.PinballFX
{
	/// <summary>
	/// Polls for the Pinball FX2 process, searches for the DMD display grabs pixels off
	/// the display, even if it's hidden (e.g. off the screen or behind the playfield).
	/// </summary>
	/// <remarks>
	/// Can be launched any time. Will wait with sending frames until Pinball FX2 is
	/// launched and stop sending when it exits.
	/// </remarks>
	public abstract class PinballFXGrabber : AbstractSource, IColoredGray2Source
	{
		protected abstract string GetProcessName();

		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		/// <summary>
		/// Wait time between polls for the Pinball FX2 process. Stops polling as soon
		/// as the process is found.
		///
		/// Can be set quite high, just about as long as it takes for Pinball FX2 to launch
		/// and load a game.
		/// </summary>
		public TimeSpan PollForProcessDelay { get; set; } = TimeSpan.FromSeconds(10);

		/// <summary>
		/// Frequency with which frames are pulled off the display.
		/// </summary>
		public double FramesPerSecond { get; set; } = 60;

		public int CropLeft { get; set; } = 4;
		public int CropTop { get; set; } = 4;
		public int CropRight { get; set; } = 4;
		public int CropBottom { get; set; } = 4;

		private IConnectableObservable<ColoredFrame> _framesColoredGray2;

		private IDisposable _capturer;
		private IntPtr _handle;
		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Waits for the Pinball FX2 process and DMD window.
		/// </summary>
		private void StartPolling()
		{
			Logger.Info("Waiting for {0} to spawn...", Name);
			var success = new Subject<Unit>();
			Observable
				.Timer(TimeSpan.Zero, PollForProcessDelay)
				.TakeUntil(success)
				.Subscribe(x => {
					_handle = FindDmdHandle();
					if (_handle != IntPtr.Zero) {
						StartCapturing();
						success.OnNext(Unit.Default);
					}
				});
		}

		/// <summary>
		/// Starts sending frames.
		/// </summary>
		private void StartCapturing()
		{
			_capturer = _framesColoredGray2.Connect();
			_onResume.OnNext(Unit.Default);
		}

		/// <summary>
		/// Stops sending frames because we couldn't aquire the DMD handle anymore,
		/// usually because Pinball FX2 was closed.
		/// </summary>
		private void StopCapturing()
		{
			// TODO send blank frame
			_capturer.Dispose();
			_onPause.OnNext(Unit.Default);
			StartPolling();
		}

		public IObservable<ColoredFrame> GetColoredGray2Frames()
		{
			double lastHue = 0;
			Color[] palette = null;
            int index = -1;

			if (_framesColoredGray2 == null) {
				var gridProcessor = new GridProcessor { Spacing = 1d };
				Logger.Info("Capturing at {0} frames per second...", FramesPerSecond);
				_framesColoredGray2 = Observable.Interval(TimeSpan.FromMilliseconds(1000d / FramesPerSecond))
					.Select(x => CaptureWindow())
					.Where(bmp => bmp != null)
					.Select(bmp => TransformationUtil.Transform(bmp, new Dimensions(128, 32), ResizeMode.Stretch, false, false))
					.Select(bmp => {
						double hue;
						var frame = ImageUtil.ConvertToGray2(bmp, 0.025, 0.3, out hue);
						if (palette == null || Math.Abs(hue - lastHue) > 0.01) {
							byte r, g, b;
							ColorUtil.HslToRgb(hue, 1, 0.5, out r, out g, out b);
							var color = Color.FromRgb(r, g, b);
							palette = ColorUtil.GetPalette(new[]{ Colors.Black, color }, 4);
							lastHue = hue;
						}

						var dim = new Dimensions(bmp.PixelWidth, bmp.PixelHeight);
						return new ColoredFrame(dim, FrameUtil.Split(dim, 2, frame), palette, index);
					})
					.Publish();

				StartPolling();
			}
			return _framesColoredGray2;
		}

		public BitmapSource CaptureWindow()
		{
			NativeCapture.RECT rc;
			GetWindowRect(_handle, out rc);

			// rect contains 0 values if handler not available anymore
			if (rc.Width == 0 || rc.Height == 0) {
				Logger.Debug("Handle lost, stopping capture.");
				_handle = IntPtr.Zero;
				StopCapturing();
				return null;
			}

			using (var bmp = new Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
				using (var gfxBmp = Graphics.FromImage(bmp)) {
					var hdcBitmap = gfxBmp.GetHdc();
					try {
						var succeeded = PrintWindow(_handle, hdcBitmap, 0);
						if (!succeeded) {
							Logger.Error("Could not retrieve image data from handle {0}", _handle);
							return null;
						}
					} finally {
						gfxBmp.ReleaseHdc(hdcBitmap);
					}
					return Convert(bmp);
				}
			}
		}

		private IntPtr FindDmdHandle()
		{
			foreach (var proc in Process.GetProcessesByName(GetProcessName())) {
				var handles = GetRootWindowsOfProcess(proc.Id);
				foreach (var handle in handles) {
					NativeCapture.RECT rc;
					GetWindowRect(handle, out rc);
					if (rc.Width == 0 || rc.Height == 0) {
						continue;
					}
					var ar = rc.Width / rc.Height;
					if (ar >= 3 && ar < 4.2) {
						return handle;
					}
				}
				Logger.Warn("{0} process found (pid {1}) but DMD not. No game running?", GetProcessName(), proc.Id);
			}
			return IntPtr.Zero;
		}

		private static IEnumerable<IntPtr> GetRootWindowsOfProcess(int pid)
		{
			var rootWindows = GetChildWindows(IntPtr.Zero);
			var dsProcRootWindows = new List<IntPtr>();
			foreach (var hWnd in rootWindows)
			{
				uint lpdwProcessId;
				GetWindowThreadProcessId(hWnd, out lpdwProcessId);
				if (lpdwProcessId == pid)
				{
					dsProcRootWindows.Add(hWnd);
				}
			}
			return dsProcRootWindows;
		}

		private static IEnumerable<IntPtr> GetChildWindows(IntPtr parent)
		{
			var result = new List<IntPtr>();
			var listHandle = GCHandle.Alloc(result);
			try
			{
				Win32Callback childProc = EnumWindow;
				EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
			}
			finally
			{
				if (listHandle.IsAllocated)
				{
					listHandle.Free();
				}
			}
			return result;
		}

		private static bool EnumWindow(IntPtr handle, IntPtr pointer)
		{
			var gch = GCHandle.FromIntPtr(pointer);
			var list = gch.Target as List<IntPtr>;
			if (list == null)
			{
				throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
			}
			list.Add(handle);
			// You can modify this to check to see if you want to cancel the operation, then return a null here
			return true;
		}

		public BitmapSource Convert(Bitmap bitmap)
		{
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
			var bitmapSource = BitmapSource.Create(bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr32, null, bitmapData.Scan0, bitmapData.Stride*bitmapData.Height, bitmapData.Stride);

			bitmap.UnlockBits(bitmapData);
			bitmapSource.Freeze(); // make it readable on any thread

			// crop border
			var cropLeft = Math.Max(0, CropLeft);
			var cropTop = Math.Max(0, CropTop);
			var cropRight = Math.Max(0, CropRight);
			var cropBottom = Math.Max(0, CropBottom);
			if (bitmapSource.PixelWidth - cropLeft - cropRight <= 0) {
				throw new CropRectangleOutOfRangeException("With a width of " + bitmapSource.PixelWidth + ", left crop of " + cropLeft + " and right crop of " + cropRight + ", there is no surface left to grab.");
			}
			if (bitmapSource.PixelHeight - cropTop - cropBottom <= 0) {
				throw new CropRectangleOutOfRangeException("With a height of " + bitmapSource.PixelHeight + ", top crop of " + cropTop + " and bottom crop of " + cropBottom + ", there is no surface left to grab.");
			}
			var rect = new Int32Rect(cropLeft, cropTop, bitmapSource.PixelWidth - cropLeft - cropRight, bitmapSource.PixelHeight - cropTop - cropBottom);

			var img = new CroppedBitmap(bitmapSource, rect);
			img.Freeze();
			return img;
		}

		#region Dll Imports

		public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetWindowRect(IntPtr hWnd, out NativeCapture.RECT lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);

		[DllImport("user32.dll")]
		internal static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

		[DllImport("gdi32.dll")]
		internal static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nReghtRect, int nBottomRect);

		[DllImport("user32.dll")]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

		#endregion

		private static void Dump(BitmapSource bmp, string filePath)
		{
			using (var fileStream = new FileStream(filePath, FileMode.Create))
			{
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bmp));
				encoder.Save(fileStream);
			}
		}
	}

	public class CropRectangleOutOfRangeException : ArgumentException
	{
		public CropRectangleOutOfRangeException(string message) : base(message)
		{
		}
	}
}
