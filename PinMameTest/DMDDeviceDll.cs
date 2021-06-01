using NLog;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PinMameTest
{
	public static class NativeLibrary
	{
		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);
	}

	/* Unimplemented methods:
	[DllImport("dmddevice", EntryPoint = "Console_Data", CallingConvention = CallingConvention.Cdecl)]
	public static extern void ConsoleData(byte data);

	[DllImport("dmddevice", EntryPoint = "Render_PM_Alphanumeric_Frame", CallingConvention = CallingConvention.Cdecl)]
	public static extern void RenderAlphaNum(NumericalLayout numericalLayout, IntPtr seg_data, IntPtr seg_data2);

	[DllImport("dmddevice", EntryPoint = "Set_4_Colors_Palette", CallingConvention = CallingConvention.Cdecl)]
	public static extern void SetGray2Palette(Rgb24 color0, Rgb24 color33, Rgb24 color66, Rgb24 color100);

	[DllImport("dmddevice", EntryPoint = "Set_16_Colors_Palette", CallingConvention = CallingConvention.Cdecl)]
	public static extern void SetGray4Palette(IntPtr palette);
	*/

	public class DMDDeviceDll : IDisposable
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int OpenCloseDelegate();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void RenderDelegate(ushort width, ushort height, IntPtr currbuffer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void RenderAlphaNumericDelegate(NumericalLayout numericalLayout, IntPtr seg_data, IntPtr seg_data2);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void GameSettingsDelegate(string gameName, ulong hardwareGeneration, IntPtr options);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int CreateDeviceDelegate();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int OpenCloseDeviceDelegate(int id);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void RenderDeviceDelegate(int id, ushort width, ushort height, IntPtr currbuffer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void RenderAlphaNumericDeviceDelegate(int id, NumericalLayout numericalLayout, IntPtr seg_data, IntPtr seg_data2);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void GameSettingsDeviceDelegate(int id, string gameName, ulong hardwareGeneration, IntPtr options);

		private IntPtr _dllhandle = IntPtr.Zero;
		private readonly OpenCloseDelegate _open = null;
		private readonly OpenCloseDelegate _close = null;
		private readonly RenderDelegate _renderRgb24 = null;
		private readonly RenderDelegate _renderGray4 = null;
		private readonly RenderDelegate _renderGray2 = null;
		private readonly RenderAlphaNumericDelegate _renderAlphaNumeric = null;
		private readonly GameSettingsDelegate _gameSettings = null;
		private readonly CreateDeviceDelegate _createDevice = null;
		private readonly OpenCloseDeviceDelegate _openDevice = null;
		private readonly OpenCloseDeviceDelegate _closeDevice = null;
		private readonly RenderDeviceDelegate _renderRgb24Device = null;
		private readonly RenderDeviceDelegate _renderGray4Device = null;
		private readonly RenderDeviceDelegate _renderGray2Device = null;
		private readonly RenderAlphaNumericDeviceDelegate _renderAlphaNumericDevice = null;
		private readonly GameSettingsDeviceDelegate _gameSettingsDevice = null;
		private int _id = -1;

		public DMDDeviceDll(string basename = "dmddevice", bool errorOnMissing = true)
		{
			string libraryName = basename + ".dll";
			// if (Environment.Is64BitProcess) libraryName = basename + "64.dll";
			var fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), libraryName);
			_dllhandle = NativeLibrary.LoadLibrary(fullPath);
			if (_dllhandle != IntPtr.Zero)
			{
				LogManager.GetCurrentClassLogger().Info("Loaded {0} from {1} to create a virtual DMD", libraryName, fullPath);
				var openHandle = NativeLibrary.GetProcAddress(_dllhandle, "Open");
				if (openHandle != IntPtr.Zero) _open = (OpenCloseDelegate)Marshal.GetDelegateForFunctionPointer(openHandle, typeof(OpenCloseDelegate));
				var closeHandle = NativeLibrary.GetProcAddress(_dllhandle, "Close");
				if (closeHandle != IntPtr.Zero) _close = (OpenCloseDelegate)Marshal.GetDelegateForFunctionPointer(closeHandle, typeof(OpenCloseDelegate));
				var renderGray2Handle = NativeLibrary.GetProcAddress(_dllhandle, "Render_4_Shades");
				if (renderGray2Handle != IntPtr.Zero) _renderGray2 = (RenderDelegate)Marshal.GetDelegateForFunctionPointer(renderGray2Handle, typeof(RenderDelegate));
				var renderGray4Handle = NativeLibrary.GetProcAddress(_dllhandle, "Render_16_Shades");
				if (renderGray4Handle != IntPtr.Zero) _renderGray4 = (RenderDelegate)Marshal.GetDelegateForFunctionPointer(renderGray4Handle, typeof(RenderDelegate));
				var renderRgb24Handle = NativeLibrary.GetProcAddress(_dllhandle, "Render_RGB24");
				if (renderRgb24Handle != IntPtr.Zero) _renderRgb24 = (RenderDelegate)Marshal.GetDelegateForFunctionPointer(renderRgb24Handle, typeof(RenderDelegate));
				var renderAlphaNumericHandle = NativeLibrary.GetProcAddress(_dllhandle, "Render_PM_Alphanumeric_Frame");
				if (renderAlphaNumericHandle != IntPtr.Zero) _renderAlphaNumeric = (RenderAlphaNumericDelegate)Marshal.GetDelegateForFunctionPointer(renderAlphaNumericHandle, typeof(RenderAlphaNumericDelegate));
				var gameSettingsHandle = NativeLibrary.GetProcAddress(_dllhandle, "PM_GameSettings");
				if (gameSettingsHandle != IntPtr.Zero) _gameSettings = (GameSettingsDelegate)Marshal.GetDelegateForFunctionPointer(gameSettingsHandle, typeof(GameSettingsDelegate));
				var createDeviceHandle = NativeLibrary.GetProcAddress(_dllhandle, "Create_Device");
				if (createDeviceHandle != IntPtr.Zero) _createDevice = (CreateDeviceDelegate)Marshal.GetDelegateForFunctionPointer(createDeviceHandle, typeof(CreateDeviceDelegate));
				var openDeviceHandle = NativeLibrary.GetProcAddress(_dllhandle, "Open_Device");
				if (openDeviceHandle != IntPtr.Zero) _openDevice = (OpenCloseDeviceDelegate)Marshal.GetDelegateForFunctionPointer(openDeviceHandle, typeof(OpenCloseDeviceDelegate));
				var closeDeviceHandle = NativeLibrary.GetProcAddress(_dllhandle, "Close_Device");
				if (closeDeviceHandle != IntPtr.Zero) _closeDevice = (OpenCloseDeviceDelegate)Marshal.GetDelegateForFunctionPointer(closeDeviceHandle, typeof(OpenCloseDeviceDelegate));
				var renderGray2DeviceHandle = NativeLibrary.GetProcAddress(_dllhandle, "Render_4_Shades_Device");
				if (renderGray2DeviceHandle != IntPtr.Zero) _renderGray2Device = (RenderDeviceDelegate)Marshal.GetDelegateForFunctionPointer(renderGray2DeviceHandle, typeof(RenderDeviceDelegate));
				var renderGray4DeviceHandle = NativeLibrary.GetProcAddress(_dllhandle, "Render_16_Shades_Device");
				if (renderGray4DeviceHandle != IntPtr.Zero) _renderGray4Device = (RenderDeviceDelegate)Marshal.GetDelegateForFunctionPointer(renderGray4DeviceHandle, typeof(RenderDeviceDelegate));
				var renderRgb24DeviceHandle = NativeLibrary.GetProcAddress(_dllhandle, "Render_RGB24_Device");
				if (renderRgb24DeviceHandle != IntPtr.Zero) _renderRgb24Device = (RenderDeviceDelegate)Marshal.GetDelegateForFunctionPointer(renderRgb24DeviceHandle, typeof(RenderDeviceDelegate));
				var renderAlphaNumericDeviceHandle = NativeLibrary.GetProcAddress(_dllhandle, "Render_PM_Alphanumeric_Frame_Device");
				if (renderAlphaNumericDeviceHandle != IntPtr.Zero) _renderAlphaNumericDevice = (RenderAlphaNumericDeviceDelegate)Marshal.GetDelegateForFunctionPointer(renderAlphaNumericDeviceHandle, typeof(RenderAlphaNumericDeviceDelegate));
				var gameSettingsDeviceHandle = NativeLibrary.GetProcAddress(_dllhandle, "PM_GameSettings_Device");
				if (gameSettingsDeviceHandle != IntPtr.Zero) _gameSettingsDevice = (GameSettingsDeviceDelegate)Marshal.GetDelegateForFunctionPointer(gameSettingsDeviceHandle, typeof(GameSettingsDeviceDelegate));
			}
			else
			{
				if (errorOnMissing)
				{
					LogManager.GetCurrentClassLogger().Error("Failed to load {0} from {1}", libraryName, fullPath);
				}
				else
				{
					LogManager.GetCurrentClassLogger().Info("{0} was not loaded since it is not available from {1}", libraryName, fullPath);
				}
			}
		}

		public void Dispose()
		{
			if (_dllhandle != IntPtr.Zero)
			{
				LogManager.GetCurrentClassLogger().Info("Disposing DMD dynamic link library");
				NativeLibrary.FreeLibrary(_dllhandle);
			}
			_dllhandle = IntPtr.Zero;
		}

		~DMDDeviceDll()
		{
			if (_dllhandle != IntPtr.Zero)
				LogManager.GetCurrentClassLogger().Error("DMD dynamic link library was not disposed before destructor call");
			Dispose();
		}

		public int Open()
		{
			if (_createDevice != null && _openDevice != null)
			{
				_id = _createDevice();
				return _openDevice(_id);
			}
			if (_open != null) return _open();
			return 0;
		}

		public int Close()
		{
			if (_id >= 0 && _closeDevice != null) return _closeDevice(_id);
			if (_close != null) return _close();
			return 0;
		}

		public void RenderRgb24(ushort width, ushort height, IntPtr currbuffer)
		{
			if (_id >= 0) _renderRgb24Device?.Invoke(_id, width, height, currbuffer); else _renderRgb24?.Invoke(width, height, currbuffer);
		}

		public void RenderGray4(ushort width, ushort height, IntPtr currbuffer)
		{
			if (_id >= 0) _renderGray4Device?.Invoke(_id, width, height, currbuffer); else _renderGray4?.Invoke(width, height, currbuffer);
		}

		public void RenderGray2(ushort width, ushort height, IntPtr currbuffer)
		{
			if (_id >= 0) _renderGray2Device?.Invoke(_id, width, height, currbuffer); else _renderGray2?.Invoke(width, height, currbuffer);
		}

		public void RenderAlphaNumeric(NumericalLayout numericalLayout, IntPtr seg_data, IntPtr seg_data2)
		{
			if (_id >= 0) _renderAlphaNumericDevice?.Invoke(_id, numericalLayout, seg_data, seg_data2); else _renderAlphaNumeric?.Invoke(numericalLayout, seg_data, seg_data2);
		}

		public void GameSettings(string gameName, ulong hardwareGeneration, PMoptions options)
		{
			IntPtr ptr = Marshal.AllocHGlobal(19 * sizeof(int));
			Marshal.StructureToPtr(options, ptr, true);
			if (_id >= 0) _gameSettingsDevice?.Invoke(_id, gameName, hardwareGeneration, ptr); else _gameSettings?.Invoke(gameName, hardwareGeneration, ptr);
			Marshal.FreeHGlobal(ptr);
		}

		public enum NumericalLayout
		{
			None,
			__2x16Alpha,
			__2x20Alpha,
			__2x7Alpha_2x7Num,
			__2x7Alpha_2x7Num_4x1Num,
			__2x7Num_2x7Num_4x1Num,
			__2x7Num_2x7Num_10x1Num,
			__2x7Num_2x7Num_4x1Num_gen7,
			__2x7Num10_2x7Num10_4x1Num,
			__2x6Num_2x6Num_4x1Num,
			__2x6Num10_2x6Num10_4x1Num,
			__4x7Num10,
			__6x4Num_4x1Num,
			__2x7Num_4x1Num_1x16Alpha,
			__1x16Alpha_1x16Num_1x7Num
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PMoptions
		{
			public int Red, Green, Blue;
			public int Perc66, Perc33, Perc0;
			public int DmdOnly, Compact, Antialias;
			public int Colorize;
			public int Red66, Green66, Blue66;
			public int Red33, Green33, Blue33;
			public int Red0, Green0, Blue0;
		}

		[StructLayout(LayoutKind.Sequential), Serializable]
		public struct Rgb24
		{
			public char Red;
			public char Green;
			public char Blue;
		}

	}
}
