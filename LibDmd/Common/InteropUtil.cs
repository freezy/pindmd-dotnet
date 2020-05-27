using System;
using System.Runtime.InteropServices;

namespace LibDmd.Common
{
	public class InteropUtil
	{
		public static ushort[] ReadUInt16Array(IntPtr data, int length)
		{
			var buffer = new ushort[length];
			var uint16Buffer = new byte[length * 2];
			Marshal.Copy(data, uint16Buffer, 0, length * 2);
			var pos = 0;
			for (var i = 0; i < length * 2; i += 2) {
				buffer[pos++] = BitConverter.ToUInt16(uint16Buffer, i);
			}
			return buffer;
		}
	}
}
