using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using LibDmd.Frame;
using LibDmd.Output;
using NLog;

namespace LibDmd.Common
{
	/// <summary>
	/// The parent class for windows ("virtual displays") that resize only to
	/// a given aspect ratio.
	/// </summary>
	public abstract class VirtualDisplay : Window
	{
		/// <summary>
		/// If true, the DMD stays on top of all other application windows.
		/// </summary>
		public bool AlwaysOnTop { get; set; }

		public abstract IVirtualControl VirtualControl { get; }

		public BehaviorSubject<VirtualDisplayPosition> PositionChanged;
		public readonly ISubject<VirtualDisplayPosition> WindowResized = new Subject<VirtualDisplayPosition>();

		public bool IgnoreAspectRatio
		{
			get => _ignoreAr;
			set {
				_ignoreAr = value;
				if (VirtualControl != null) {
					VirtualControl.IgnoreAspectRatio = value;
				}
			}
		}

		public bool LockHeight { get; set; }
		public bool Resizing { get; private set; }

		private bool _ignoreAr;
		private double _aspectRatio;
		private bool? _adjustingHeight;

		protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected VirtualDisplay()
		{
			MouseEnter += (sender, args) => Resources["GripColor"] = Brushes.White;
			MouseLeave += (sender, args) => Resources["GripColor"] = Brushes.Transparent;
		}

		protected void Initialize()
		{
			DataContext = this;
			SourceInitialized += Window_SourceInitialized;
			MouseDown += Window_MouseDown;
			Deactivated += Window_Deactivated;
			LocationChanged += LocationChanged_Event;
			SizeChanged += LocationChanged_Event;
			ShowActivated = false;
			VirtualControl.Host = this;
			PositionChanged = new BehaviorSubject<VirtualDisplayPosition>(new VirtualDisplayPosition(Left, Top, Width, Height));
			ForceOnTop();
		}

		public void SetDimensions(Dimensions dim)
		{
			if (_ignoreAr) {
				return;
			}
			Dispatcher.Invoke(() => {
				_aspectRatio = dim.AspectRatio;
				if (LockHeight) {
					Width = Height * _aspectRatio;
				} else {
					Height = Width / _aspectRatio;
				}
			});
		}

		private void ForceOnTop()
		{
			var window = (Window)this;
			window.Topmost = true;

			var processes = Process.GetProcesses();
			var b2s = processes.FirstOrDefault(process => process.ProcessName == "B2SBackglassServerEXE");
			if (b2s != null) {
				Logger.Info("Found B2S, moving behind DMD.");
				SetWindowPos(b2s.MainWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
			}
		}

		private void LocationChanged_Event(object sender, EventArgs e)
		{
			PositionChanged.OnNext(new VirtualDisplayPosition(Left, Top, Width, Height));
		}

		public static Point GetMousePosition() // mouse position relative to screen
		{
			var w32Mouse = new Win32Point();
			GetCursorPos(ref w32Mouse);
			return new Point(w32Mouse.X, w32Mouse.Y);
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left) {
				DragMove();
			}
		}

		private void Window_Deactivated(object sender, EventArgs e)
		{
			if (AlwaysOnTop) {
				ForceOnTop();
			}
		}

		private void Window_SourceInitialized(object sender, EventArgs ea)
		{
			var hwndSource = (HwndSource)PresentationSource.FromVisual((Window)sender);
			hwndSource?.AddHook(DragHook);

			_aspectRatio = Width / Height;
		}

		private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (_ignoreAr) {
				return IntPtr.Zero;
			}

			//if (!new[] {132, 32, 512 }.Contains(msg)) {
			//	Logger.Info("hwndSource event: {0}", msg);
			//}

			switch ((WM)msg) {
				case WM.WindowPosChanging: {
						var pos = (WindowPos)Marshal.PtrToStructure(lParam, typeof(WindowPos));

						if ((pos.flags & (int)SWP.NoMove) != 0) {
							return IntPtr.Zero;
						}

						var wnd = (Window)HwndSource.FromHwnd(hwnd)?.RootVisual;
						if (wnd == null) {
							return IntPtr.Zero;
						}

						// determine what dimension is changed by detecting the mouse position relative to the
						// window bounds. if gripped in the corner, either will work.
						if (!_adjustingHeight.HasValue) {
							var p = GetMousePosition();

							var diffWidth = Math.Min(Math.Abs(p.X - pos.x), Math.Abs(p.X - pos.x - pos.cx));
							var diffHeight = Math.Min(Math.Abs(p.Y - pos.y), Math.Abs(p.Y - pos.y - pos.cy));

							_adjustingHeight = diffHeight > diffWidth;
						}

						if (_adjustingHeight.Value) {
							pos.cy = (int)(pos.cx / _aspectRatio); // adjusting height to width change

						} else {
							pos.cx = (int)(pos.cy * _aspectRatio); // adjusting width to height change
						}
						Marshal.StructureToPtr(pos, lParam, true);
						handled = true;
					}
					break;

				case WM.Sizing:
					Resizing = true;
					break;

				case WM.ExitSizeMove:
					_adjustingHeight = null; // reset adjustment dimension and detect again next time window is resized
					Resizing = false;
					WindowResized.OnNext(new VirtualDisplayPosition(Left, Top, Width, Height));
					break;

			}
			return IntPtr.Zero;
		}


		[StructLayout(LayoutKind.Sequential)]
		internal struct WindowPos
		{
			public IntPtr hwnd;
			public IntPtr hwndInsertAfter;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public int flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct Win32Point
		{
			public int X;
			public int Y;
		};

		internal enum SWP
		{
			NoMove = 0x0002
		}

		internal enum WM
		{
			WindowPosChanging = 0x0046,
			Sizing = 0x0214,
			ExitSizeMove = 0x0232,
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetCursorPos(ref Win32Point pt);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);
		private const int HWND_TOPMOST = -1;
		private const int HWND_BOTTOM = 1;
		private const int HWND_NOTOPMOST = -2;
		private const int SWP_NOMOVE = 0x0002;
		private const int SWP_NOSIZE = 0x0001;
	}

	public class VirtualDisplayPosition
	{
		public double Left { get; set; }
		public double Top { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }

		public VirtualDisplayPosition()
		{
		}

		public VirtualDisplayPosition(double left, double top, double width, double height)
		{
			Left = left;
			Top = top;
			Width = width;
			Height = height;
		}

		public override string ToString()
		{
			return $"{Width}x{Height}@{Left}/{Top}";
		}
	}

	public interface IVirtualControl : IDestination
	{
		bool IgnoreAspectRatio { get; set; }
		VirtualDisplay Host { get; set; }
	}
}
