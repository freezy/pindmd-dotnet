using System;
using System.Runtime.InteropServices;

namespace LibDmd.Input
{
	public abstract class MemoryGrabberBase : AbstractSource
	{

		#region Dll Imports

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		protected static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, int size, IntPtr lpNumberOfBytesRead);

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		protected static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, IntPtr lpNumberOfBytesWritten);

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		protected static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		// Process access rights
		protected const int PROCESS_VM_OPERATION = 0x0008;
		protected const int PROCESS_VM_READ = 0x0010;
		protected const int PROCESS_VM_WRITE = 0x0020;
		protected const int SYNCHRONIZE = 0x00100000;

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		protected static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int flAllocationType, int flProtect);

		#endregion

	}
}
