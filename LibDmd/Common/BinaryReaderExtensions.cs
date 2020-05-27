using System;
using System.IO;

namespace LibDmd.Common
{
	/// <summary>
	/// Reader functions for Big Endian integers.
	/// </summary>
	/// 
	/// <remarks>
	/// Note this MODIFIES THE GIVEN ARRAY then returns a reference to the modified array.
	/// </remarks>
	public static class BinaryReaderExtensions
	{
		public static byte[] Reverse(this byte[] b)
		{
			Array.Reverse(b);
			return b;
		}

		/// <summary>
		/// Reads a 16-bit unsigned integer as Big Endian from the binary reader.
		/// </summary>
		/// <returns>unsigned 16-bit integer</returns>
		public static ushort ReadUInt16BE(this BinaryReader binRdr)
		{
			return BitConverter.ToUInt16(binRdr.ReadBytesRequired(sizeof(ushort)).Reverse(), 0);
		}

		/// <summary>
		/// Reads a 16-bit signed integer as Big Endian from the binary reader.
		/// </summary>
		/// <returns>signed 16-bit integer</returns>
		public static short ReadInt16BE(this BinaryReader binRdr)
		{
			return BitConverter.ToInt16(binRdr.ReadBytesRequired(sizeof(short)).Reverse(), 0);
		}

		/// <summary>
		/// Reads a 32-bit unsigned integer as Big Endian from the binary reader.
		/// </summary>
		/// <returns>unsigned 32-bit integer</returns>
		public static uint ReadUInt32BE(this BinaryReader binRdr)
		{
			return BitConverter.ToUInt32(binRdr.ReadBytesRequired(sizeof(uint)).Reverse(), 0);
		}

		/// <summary>
		/// Reads a 32-bit signed integer as Big Endian from the binary reader.
		/// </summary>
		/// <returns>signed 32-bit integer</returns>
		public static int ReadInt32BE(this BinaryReader binRdr)
		{
			return BitConverter.ToInt32(binRdr.ReadBytesRequired(sizeof(int)).Reverse(), 0);
		}

		/// <summary>
		/// Reads a 64-bit unsigned integer as Big Endian from the binary reader.
		/// </summary>
		/// <returns>unsigned 64-bit integer</returns>
		public static ulong ReadUInt64BE(this BinaryReader binRdr)
		{
			return BitConverter.ToUInt64(binRdr.ReadBytesRequired(sizeof(ulong)).Reverse(), 0);
		}

		/// <summary>
		/// Reads a 64-bit signed integer as Big Endian from the binary reader.
		/// </summary>
		/// <returns>signed 64-bit integer</returns>
		public static long ReadInt64BE(this BinaryReader binRdr)
		{
			return BitConverter.ToInt64(binRdr.ReadBytesRequired(sizeof(long)).Reverse(), 0);
		}

		public static byte[] ReadBytesRequired(this BinaryReader binRdr, int byteCount)
		{
			var result = binRdr.ReadBytes(byteCount);

			if (result.Length != byteCount)
				throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));

			return result;
		}
	}
}
