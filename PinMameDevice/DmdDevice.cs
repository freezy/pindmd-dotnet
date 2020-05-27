using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Media;
using LibDmd.Common;
using LibDmd.DmdDevice;
using LibDmd.Frame;
using NLog;

namespace PinMameDevice
{
	/// <summary>
	/// Äs DLL womr cha ubr C/C++ inäladä und wo vo VPinMAME bruicht wird um DMD
	/// datä z schickä. Drbi wird äs API implementiärt.
	/// </summary>
	/// <remarks>
	/// Diä Klass beinhautet fasch kä Logik sondrn tuät fascht auäs diräkt a
	/// <see cref="LibDmd.DmdDevice.DmdDevice"/> weytrleitä.
	/// </remarks>
	/// <see cref="https://sourceforge.net/p/pinmame/code/HEAD/tree/trunk/ext/dmddevice/dmddevice.h"/>
	public static class DmdDevice
	{
		private static readonly DmdFrame _dmdFrame = new DmdFrame();
		private static readonly RawFrame RawFrame = new RawFrame();
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		static readonly IDmdDevice _dmdDevice = new LibDmd.DmdDevice.DmdDevice();
		static readonly LinkedList<char> CData = new LinkedList<char>();


		// int Open()
		[DllExport("Open", CallingConvention = CallingConvention.Cdecl)]
		public static int Open()
		{
			Logger.Info("[vpm] Open()");
			// wird ignoriärt wiu mr wartit bis diä ganzi Konfig ibärä isch.
			return 1;
		}

		// bool Close()
		[DllExport("Close", CallingConvention = CallingConvention.Cdecl)]
		public static bool Close()
		{
			Logger.Info("[vpm] Close()");
			_dmdDevice.Close();
			return true;
		}

		// void PM_GameSettings(const char* GameName, UINT64 HardwareGeneration, const PMoptions &Options)
		[DllExport("PM_GameSettings", CallingConvention = CallingConvention.Cdecl)]
		public static void GameSettings(string gameName, ulong hardwareGeneration, IntPtr options)
		{
			var opt = (PMoptions)Marshal.PtrToStructure(options, typeof(PMoptions));
			Logger.Info("[vpm] PM_GameSettings({0})", opt.Colorize);
			_dmdDevice.SetColorize(opt.Colorize != 0);
			_dmdDevice.SetGameName(gameName);
			_dmdDevice.SetColor(Color.FromRgb((byte)(opt.Red), (byte)(opt.Green), (byte)(opt.Blue)));
			_dmdDevice.Init();
		}

		// void Console_Data(UINT8 data)
		[DllExport("Console_Data", CallingConvention = CallingConvention.Cdecl)]
		public static void ConsoleData(byte data)
		{
			// Dä schickt immr eis Byte abr eigentlich wettr Bleck vo viär Bytes,
			// d.h miär mind ihs merkä was diä letschtä drii Bytes gsi sind um eppis
			// schlays chenna witr z schickä.
			// Wemmer diä viär Bytes de mau hett isch dr erschti Wärt immer äs P und
			// diä zwe druif sind Textzeichä womr i Hex muäss umwandlä. Am Schluss
			// chunnt de nu ä ni i Zihlä.
			CData.AddLast((char)data);
			if (CData.Count <= 4) {
				// het nunig aui wärt
				return;
			}
			CData.RemoveFirst();
			if (CData.First.Value == 'P') {
				var num = new string(new[] { CData.First.Next.Value, CData.First.Next.Next.Value });
				try {
					_dmdDevice.LoadPalette(Convert.ToUInt32(num, 16));
				} catch (FormatException e) {
					Logger.Warn(e, "Could not parse \"{0}\" as hex number.", num);
				}
			}
		}

		// void Render_RGB24(UINT16 width, UINT16 height, Rgb24 *currbuffer)
		[DllExport("Render_RGB24", CallingConvention = CallingConvention.Cdecl)]
		public static void RenderRgb24(ushort width, ushort height, IntPtr currbuffer)
		{
			var dim = new Dimensions(width, height);
			var frameSize = dim.Surface * 3;
			var frame = new byte[frameSize];
			Marshal.Copy(currbuffer, frame, 0, frameSize);
			_dmdDevice.RenderRgb24(_dmdFrame.Update(dim, frame));
		}

		// void Render_16_Shades(UINT16 width, UINT16 height, UINT8 *currbuffer)
		[DllExport("Render_16_Shades_with_Raw", CallingConvention = CallingConvention.Cdecl)]
		public static void RenderRaw4(ushort width, ushort height, IntPtr currbuffer, ushort noOfRawFrames, IntPtr currrawbuffer)
		{
			var dim = new Dimensions(width, height);
			var frameSize = dim.Surface;
			var frame = new byte[frameSize];
			Marshal.Copy(currbuffer, frame, 0, frameSize);

			var rawplanes = new byte[noOfRawFrames][];
			var planeSize = frameSize / 8;
			for (int i = 0; i < noOfRawFrames; i++)
			{
				rawplanes[i] = new byte[planeSize];
				Marshal.Copy(new IntPtr(currrawbuffer.ToInt64() + (i * planeSize)), rawplanes[i], 0, planeSize);
			}
			_dmdDevice.RenderGray4(RawFrame.Update(dim, frame, rawplanes));
		}

		// void Render_4_Shades(UINT16 width, UINT16 height, UINT8 *currbuffer)
		[DllExport("Render_4_Shades_with_Raw", CallingConvention = CallingConvention.Cdecl)]
		public static void RenderRaw2(ushort width, ushort height, IntPtr currbuffer, ushort noOfRawFrames, IntPtr currrawbuffer)
		{
			var dim = new Dimensions(width, height);
			var frameSize = dim.Surface;
			var frame = new byte[frameSize];
			Marshal.Copy(currbuffer, frame, 0, frameSize);
			var rawplanes = new byte[noOfRawFrames][];
			var planeSize = frameSize / 8;
			for (int i = 0; i < noOfRawFrames; i++)
			{
				rawplanes[i] = new byte[planeSize];
				Marshal.Copy(new IntPtr(currrawbuffer.ToInt64() + (i * planeSize)), rawplanes[i], 0, planeSize);
			}

			_dmdDevice.RenderGray2(RawFrame.Update(dim, frame, rawplanes));
		}


		// void Render_16_Shades(UINT16 width, UINT16 height, UINT8 *currbuffer)
		[DllExport("Render_16_Shades", CallingConvention = CallingConvention.Cdecl)]
		public static void RenderGray4(ushort width, ushort height, IntPtr currbuffer)
		{
			var dim = new Dimensions(width, height);
			var frameSize = dim.Surface;
			var frame = new byte[frameSize];
			Marshal.Copy(currbuffer, frame, 0, frameSize);
			_dmdDevice.RenderGray4(_dmdFrame.Update(dim, frame));
		}

		// void Render_4_Shades(UINT16 width, UINT16 height, UINT8 *currbuffer)
		[DllExport("Render_4_Shades", CallingConvention = CallingConvention.Cdecl)]
		public static void RenderGray2(ushort width, ushort height, IntPtr currbuffer)
		{
			var dim = new Dimensions(width, height);
			var frameSize = dim.Surface;
			var frame = new byte[frameSize];
			Marshal.Copy(currbuffer, frame, 0, frameSize);
			_dmdDevice.RenderGray2(_dmdFrame.Update(dim, frame));
		}

		//  void Render_PM_Alphanumeric_Frame(NumericalLayout numericalLayout, const UINT16 *const seg_data, const UINT16 *const seg_data2)
		[DllExport("Render_PM_Alphanumeric_Frame", CallingConvention = CallingConvention.Cdecl)]
		public static void RenderAlphaNum(NumericalLayout numericalLayout, IntPtr seg_data, IntPtr seg_data2)
		{
			_dmdDevice.RenderAlphaNumeric(numericalLayout, InteropUtil.ReadUInt16Array(seg_data, 64), InteropUtil.ReadUInt16Array(seg_data2, 64));
		}

		// void Set_4_Colors_Palette(Rgb24 color0, Rgb24 color33, Rgb24 color66, Rgb24 color100)
		[DllExport("Set_4_Colors_Palette", CallingConvention = CallingConvention.Cdecl)]
		public static void SetGray2Palette(Rgb24 color0, Rgb24 color33, Rgb24 color66, Rgb24 color100)
		{
			Logger.Info("[vpm] Set_4_Colors_Palette()");
			_dmdDevice.SetPalette(new[] {
				ConvertColor(color0),
				ConvertColor(color33),
				ConvertColor(color66),
				ConvertColor(color100)
			});
		}

		// void Set_16_Colors_Palette(Rgb24 *color)
		[DllExport("Set_16_Colors_Palette", CallingConvention = CallingConvention.Cdecl)]
		public static void SetGray4Palette(IntPtr palette)
		{
			Logger.Info("[vpm] Set_16_Colors_Palette()");
			var size = Marshal.SizeOf(typeof (Rgb24));

			// for some shit reason, using a loop fails compilation.
			_dmdDevice.SetPalette(new[] {
				ConvertColor(GetColorAtPosition(palette, 0, size)),
				ConvertColor(GetColorAtPosition(palette, 1, size)),
				ConvertColor(GetColorAtPosition(palette, 2, size)),
				ConvertColor(GetColorAtPosition(palette, 3, size)),
				ConvertColor(GetColorAtPosition(palette, 4, size)),
				ConvertColor(GetColorAtPosition(palette, 5, size)),
				ConvertColor(GetColorAtPosition(palette, 6, size)),
				ConvertColor(GetColorAtPosition(palette, 7, size)),
				ConvertColor(GetColorAtPosition(palette, 8, size)),
				ConvertColor(GetColorAtPosition(palette, 9, size)),
				ConvertColor(GetColorAtPosition(palette, 10, size)),
				ConvertColor(GetColorAtPosition(palette, 11, size)),
				ConvertColor(GetColorAtPosition(palette, 12, size)),
				ConvertColor(GetColorAtPosition(palette, 13, size)),
				ConvertColor(GetColorAtPosition(palette, 14, size)),
				ConvertColor(GetColorAtPosition(palette, 15, size)),
			});
		}

		private static bool IsOldWindows()
		{
			return Environment.OSVersion.Version.Major < 5;
		}

		private static Rgb24 GetColorAtPosition(IntPtr data, int pos, int size)
		{
			var p = new IntPtr(data.ToInt64() + pos*size);
			return (Rgb24) Marshal.PtrToStructure(p, typeof (Rgb24));
		}

		private static Color ConvertColor(Rgb24 color)
		{
			return Color.FromRgb((byte) color.Red, (byte) color.Green, (byte) color.Blue);
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
