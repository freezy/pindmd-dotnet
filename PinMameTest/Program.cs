using System;
using System.Runtime.InteropServices;
using System.Threading;
using PinMameDevice;

namespace PinMameTest
{
	class Program
	{
		static void Main(string[] args)
		{
			byte[] bytes = new byte[128 * 32 * 3];
			IntPtr frame = Marshal.AllocHGlobal(bytes.Length);
			for (int y = 0; y < 32; y++)
			{
				for (int x = 0; x < 128; x++)
				{
					bytes[x * 3 + y * 128 * 3] = (byte)(x * 2);
					bytes[x * 3 + y * 128 * 3 + 1] = (byte)(y * 4);
				}
			}
			Marshal.Copy(bytes, 0, frame, bytes.Length);
			var options = new DMDDeviceDll.PMoptions();
			IntPtr optPtr = Marshal.AllocHGlobal(Marshal.SizeOf(options));
			Marshal.StructureToPtr(options, optPtr, true);

			if (args.Length == 0 || args[0].Equals("VPinMame"))
			{
				Console.WriteLine("Testing PinMame device access through VPinMame COM object.");
				Console.WriteLine("[{0}] Starting...", Thread.CurrentThread.ManagedThreadId);
				var controller = new VPinMameController();
				//Controller.Run("sshtl_l7").Subscribe(status => {
				//Controller.Run("sprk_103").Subscribe(status => {
				controller.Run("tz_92").Subscribe(status =>
				{
					Console.WriteLine("[{0}] Game status: {1}", Thread.CurrentThread.ManagedThreadId, status);
				});
				Thread.Sleep(10000);
			}
			else if (args.Length >= 1 && args[0].Equals("Project"))
			{
				Console.WriteLine("Testing PinMame device access through direct project reference (embedding).");
				var dmdId = DmdDevice.CreateDevice();
				DmdDevice.OpenDevice(dmdId);
				DmdDevice.GameSettingsDevice(dmdId, "test_direct", 0, optPtr);
				DmdDevice.RenderRgb24Device(dmdId, 128, 32, frame);
				Thread.Sleep(10000);
				DmdDevice.CloseDevice(dmdId);
			}
			else if (args.Length >= 1 && args[0].Equals("DLL"))
			{
				Console.WriteLine("Testing PinMame device access through DLL loading.");
				var dmdFromDll = new DMDDeviceDll("DmdDevice", true);
				dmdFromDll.Open();
				dmdFromDll.GameSettings("test_dll", 0, options);
				dmdFromDll.RenderRgb24(128, 32, frame);
				Thread.Sleep(10000);
				dmdFromDll.Close();
				dmdFromDll.Dispose();
			}

			Console.WriteLine("Done.");
			Marshal.FreeHGlobal(optPtr);
			Marshal.FreeHGlobal(frame);
		}
	}
}
