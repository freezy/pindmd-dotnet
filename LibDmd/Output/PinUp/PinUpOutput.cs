using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using LibDmd.Common;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Output.PinUp
{
	public class PinUpOutput : IBitmapDestination, IGray2Destination, IGray4Destination, IFixedSizeDestination
	{
		public string OutputFolder { get; set; }

		public string Name { get; } = "PinUP Writer";
		public bool IsAvailable { get; private set; }

		public int Width = 128;
		public int Height = 32;
		public uint Fps;

		public Dimensions FixedSize { get; } = new Dimensions(128, 32);

		private readonly IntPtr _pnt;
		private readonly string _gameName;

		public PinUpOutput(string romName)
		{
			if (romName == null) {
				throw new InvalidEnumArgumentException("ROM name must not be null.");
			}

			IsAvailable = true;

			var localPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			var assemblyFolder = Path.GetDirectoryName(localPath);
			var dllFileName = Path.Combine(assemblyFolder, "dmddevicePUP.DLL");
			var pDll = NativeMethods.LoadLibrary(dllFileName);

			if (pDll == IntPtr.Zero) {
				IsAvailable = false;
				Logger.Error("Cannot load " + dllFileName);
				return;
			}

			try {
				var pAddress = NativeMethods.GetProcAddress(pDll, "Render_RGB24");
				if (pAddress == IntPtr.Zero) {
					throw new Exception("Cannot find Render_RGB24 in dmddevicePUP.dll");
				}
				Render_RGB24 = (_dRender_RGB24)Marshal.GetDelegateForFunctionPointer(pAddress, typeof(_dRender_RGB24));

				pAddress = NativeMethods.GetProcAddress(pDll, "Open");
				if (pAddress == IntPtr.Zero) {
					throw new Exception("Cannot map function in dmddevicePUP.dll");
				}
				Open = (_dOpen)Marshal.GetDelegateForFunctionPointer(pAddress, typeof(_dOpen));

				pAddress = NativeMethods.GetProcAddress(pDll, "Close");
				if (pAddress == IntPtr.Zero) {
					throw new Exception("Cannot map function in dmddevicePUP.dll");
				}
				Close = (_dClose)Marshal.GetDelegateForFunctionPointer(pAddress, typeof(_dClose));

				//pAddress = NativeMethods.GetProcAddress(pDll, "GameSettings");
				//if (pAddress == IntPtr.Zero)
				//	throw new Exception("Cannot map function in dmddevicePUP.dll");
				//GameSettings = (_dGameSettings)Marshal.GetDelegateForFunctionPointer(pAddress, typeof(_dGameSettings)); */

				pAddress = NativeMethods.GetProcAddress(pDll, "SetGameName");
				if (pAddress == IntPtr.Zero) {
					throw new Exception("Cannot map function in dmddevicePUP.dll");
				}
				SetGameName = (_dSetGameName)Marshal.GetDelegateForFunctionPointer(pAddress, typeof(_dSetGameName));

			} catch (Exception e) {
				IsAvailable = false;
				Logger.Error(e, "[PinUpOutput] Error sending frame to PinUp, disabling.");
				return;
			}

			Logger.Info("PinUP DLL starting " + romName + "...");
			Open();

			_pnt = Marshal.AllocHGlobal(Width * Height * 3); // make a global memory pnt for DLL call
			_gameName = romName;

			Marshal.Copy(Encoding.ASCII.GetBytes(_gameName), 0, _pnt, _gameName.Length); // convert to bytes to make DLL call work?
			SetGameName(_pnt, _gameName.Length); // external PUP dll call
		}

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		//Render Bitmap gets called by dmdext console.  (pinball fx2/3 type support)
		public void RenderBitmap(BmpFrame frame)
		{
			var rgb24Data = ImageUtil.ConvertToRgb24(frame.Bitmap);
			RenderRgb24(new DmdFrame(frame.Dimensions, rgb24Data));
		}

		public void Dispose()
		{
			try {
				Close();

			} catch (Exception e) {
				Logger.Warn(e, "Error closing PinUP output: {0}", e.Message);
			}

			// Marshal.FreeHGlobal(pnt);
		}

		public void ClearDisplay()
		{
			// no, we don't write a blank image.
		}

		public void RenderRgb24(DmdFrame frame)
		{
			try {
				// Copy the fram array to unmanaged memory.

				// Marshal.Copy(frame, 0, pnt, Width * Height * 3);    //crash with 128x16 so try something else
				// Render_RGB24((ushort) Width, (ushort) Height, pnt);
				Marshal.Copy(frame.Data, 0, _pnt, FixedSize.Surface * 3);
				Render_RGB24((ushort) FixedSize.Width, (ushort) FixedSize.Height, _pnt);

			} catch (Exception e) {
				IsAvailable = false;
				Logger.Error(e, "[PinUpOutput] Error sending frame to PinUp, disabling.");
			}
		}

		public void RenderGray4(DmdFrame frame)
		{
			try {
				// Render as orange palette (same as default with no PAL loaded)
				var planes = FrameUtil.Split(FixedSize, 4, frame.Data);

				var orangeframe = LibDmd.Common.FrameUtil.ConvertToRgb24(FixedSize, planes,
					ColorUtil.GetPalette(new[] {Colors.Black, Colors.OrangeRed}, 16));

				Marshal.Copy(orangeframe, 0, _pnt, FixedSize.Surface * 3);
				Render_RGB24((ushort) FixedSize.Width, (ushort) FixedSize.Width, _pnt);

			} catch (Exception e) {
				IsAvailable = false;
				Logger.Error(e, "[PinUpOutput] Error sending frame to PinUp, disabling.");
			}
		}

		public void RenderGray2(DmdFrame frame)
		{
			// 2-bit frames are rendered as 4-bit
			var gray4Data = FrameUtil.ConvertGrayToGray(frame.Data, new byte[] {0x0, 0x1, 0x4, 0xf});
			RenderGray4(new DmdFrame(frame.Dimensions, gray4Data));
		}

		public void RenderRaw(byte[] data)
		{
			try {
				Marshal.Copy(data, 0, _pnt, Width * Height * 3);
				Render_RGB24((ushort) Width, (ushort) Height, _pnt);
			} catch (Exception e) {
				IsAvailable = false;
				Logger.Error(e, "[PinUpOutput] Error sending frame to PinUp, disabling.");
			}
		}

		public void SetColor(Color color)
		{
			// ignore
		}

		public void SetPalette(Color[] colors, int index = -1)
		{
			// ignore
		}

		public void ClearPalette()
		{
			// ignore
		}

		public void ClearColor()
		{
			// ignore
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void _dRender_RGB24(ushort width, ushort height, IntPtr currbuffer);
		static _dRender_RGB24 Render_RGB24;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int _dOpen();
		static _dOpen Open;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate bool _dClose();
		static _dClose Close;

		/*  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		  private delegate void _dGameSettings([MarshalAs(UnmanagedType.LPStr)]string gameName, ulong hardwareGeneration, IntPtr options);
		  static _dGameSettings GameSettings;*/

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void _dSetGameName(IntPtr cName, int len);
		static _dSetGameName SetGameName;
	}

	static class NativeMethods
	{
		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);
	}
}
