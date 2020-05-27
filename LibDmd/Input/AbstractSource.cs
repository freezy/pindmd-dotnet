using System;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Security.Principal;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Input
{
	public abstract class AbstractSource
	{
		public BehaviorSubject<Dimensions> Dimensions { get; set; }

		public abstract string Name { get; }

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public void SetDimensions(Dimensions dim)
		{
			if (Dimensions == null) {
				return;
			}

			if (dim != Dimensions.Value) {
				Logger.Info("{2} received new dimensions: {0} => {1}.", Dimensions, dim, Name);
				Dimensions.OnNext(dim);
			}
		}

		// Set SeDebugPrivilege in our proecess token, to allow debugger
		// level access to another process's memory space.  This is used
		// for input classes that read DMD data directly from memory in
		// the target game process, such as Pinball Arcade DX11 and
		// Pinball FX3.
		protected void SetDebugPrivilege()
		{
			// Check if we're in admin mode.  If so, we won't need to do
			// anything special to add debug privileges, because an Admin
			// mode process can already have its way with other processes.
			var curIdentity = WindowsIdentity.GetCurrent();
			var myPrincipal = new WindowsPrincipal(curIdentity);
			if (myPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
			{
				return;
			}

			// open our process token
			IntPtr hProcToken;
			if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hProcToken))
			{
				// if there's no token, impersonate self to create one
				if (Marshal.GetLastWin32Error() == ERROR_NO_TOKEN)
				{
					if (!ImpersonateSelf(SecurityImpersonation))
					{
						throw new DebugPrivilegeException("Trying to get process token: ImpersonateSelf() failed with win32 error "
							+ Marshal.GetLastWin32Error());
					}
				}
				else
				{
					throw new DebugPrivilegeException("Unable to get process token; win32 error " + Marshal.GetLastWin32Error());
				}

				// try opening the token again
				if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hProcToken))
				{
					throw new DebugPrivilegeException("Unable to open process token; win32 error " + Marshal.GetLastWin32Error());
				}
			}

			// Enable debug privilege in our token.  Allow this to fail silently,
			// as we might be able to exercise the rights this privilege would have
			// granted without explicitly setting it.  We'll find out when we try
			// the protected operation, and generate suitable diagnostics at that
			// point if it fails.
			SetPrivilege(hProcToken, "SeDebugPrivilege");
			CloseHandle(hProcToken);
		}

		// enable a privilege
		private bool SetPrivilege(IntPtr hToken, string privilege)
		{
			// look up the LUID for the privilege
			LUID luid;
			if (!LookupPrivilegeValueW(null, privilege, out luid))
			{
				Logger.Log(LogLevel.Error, "Error looking up LUID for privilege " + privilege + ", win32 error "
					+ Marshal.GetLastWin32Error());
				return false;
			}

			// set the new privilege setting
			TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
			tp.PrivilegeCount = 1;
			tp.Privileges = new LUID_AND_ATTRIBUTES[1];
			tp.Privileges[0].Luid = luid;
			tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
			if (!AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
			{
				Logger.Log(LogLevel.Warn, "Error setting privilege " + privilege + "in security token, win32 error "
					+ Marshal.GetLastWin32Error());
				return false;
			}

			// success
			return true;
		}

		// native imports for adjusting the process token

		const UInt32 ERROR_NO_TOKEN = 1008;

		[DllImport("advapi32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr hToken);

		[DllImport("advapi32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool OpenThreadToken(IntPtr ThreadHandle, UInt32 DesiredAccess, bool openAsSelf, out IntPtr hToken);

		// thread access rights
		public const UInt32 STANDARD_RIGHTS_REQUIRED = (0x000F0000);
		public const UInt32 TOKEN_ASSIGN_PRIMARY = (0x0001);
		public const UInt32 TOKEN_DUPLICATE = (0x0002);
		public const UInt32 TOKEN_IMPERSONATE = (0x0004);
		public const UInt32 TOKEN_QUERY = (0x0008);
		public const UInt32 TOKEN_QUERY_SOURCE = (0x0010);
		public const UInt32 TOKEN_ADJUST_PRIVILEGES = (0x0020);
		public const UInt32 TOKEN_ADJUST_GROUPS = (0x0040);
		public const UInt32 TOKEN_ADJUST_DEFAULT = (0x0080);
		public const UInt32 TOKEN_ADJUST_SESSIONID = (0x0100);

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
		static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
		static extern IntPtr GetCurrentThread();

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
		protected static extern bool CloseHandle(IntPtr handle);

		[StructLayout(LayoutKind.Sequential)]
		struct LUID
		{
			public uint LowPart;
			public int HighPart;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct TOKEN_PRIVILEGES
		{
			public UInt32 PrivilegeCount;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
			public LUID_AND_ATTRIBUTES[] Privileges;
		}

		const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;

		[StructLayout(LayoutKind.Sequential)]
		struct LUID_AND_ATTRIBUTES
		{
			public LUID Luid;
			public UInt32 Attributes;
		}

		[DllImport("advapi32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool LookupPrivilegeValueW(string systemName, string name, out LUID luid);

		[DllImport("advapi32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool AdjustTokenPrivileges(IntPtr hToken, bool disableAll, ref TOKEN_PRIVILEGES newState,
		   UInt32 bufferLength, ref TOKEN_PRIVILEGES previousState, out UInt32 returnLength);

		[DllImport("advapi32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool AdjustTokenPrivileges(IntPtr hToken, bool disableAll, ref TOKEN_PRIVILEGES newState,
		   UInt32 bufferLength, IntPtr oldState, IntPtr oldStateLength);

		[DllImport("advapi32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool ImpersonateSelf(UInt32 securityImpersonationLevel);

		// impersonation levels
		const UInt32 SecurityAnonymous = 0;
		const UInt32 SecurityIdentification = 1;
		const UInt32 SecurityImpersonation = 2;
		const UInt32 SecurityDeletation = 3;

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		public static extern UInt32 WaitForSingleObject(IntPtr hWaitHandle, UInt32 dwMilliseconds);
		protected const UInt32 INFINITE = 0xFFFFFFFF;
		protected const UInt32 WAIT_ABANDONED = 0x00000080;
		protected const UInt32 WAIT_OBJECT_0 = 0x00000000;
		protected const UInt32 WAIT_TIMEOUT = 0x00000102;
	}

	public class DebugPrivilegeException : Exception
	{
		public DebugPrivilegeException(string details) : base(String.Format(GenericMessage, details))
		{
		}

		static String GenericMessage = "The program can't access DMD information from the target program due to "
				   + "system security restrictions.  You might be able to fix this by running dmdext "
				   + "as Administrator.  (Error details: {0})";
	}
}
