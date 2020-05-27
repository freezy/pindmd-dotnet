using System.Collections.Generic;
using System.IO;
using LibDmd.Common;
using LibDmd.Common.HeatShrink;
using System.Linq;

namespace LibDmd.Converter.Colorize
{
	public class VniAnimationFrame : AnimationFrame
	{
		public VniAnimationFrame(BinaryReader reader, int fileVersion, uint time) : base(time)
		{
			int planeSize = reader.ReadInt16BE();
			Delay = (uint) reader.ReadInt16BE();
			if (fileVersion >= 4) {
				Hash = reader.ReadUInt32BE();
			}
			BitLength = reader.ReadByte();
			Planes = new List<AnimationPlane>(BitLength);
			
			if (fileVersion < 3) {
				ReadPlanes(reader, planeSize);

			} else {
				var compressed = reader.ReadByte() != 0;
				if (!compressed) {
					ReadPlanes(reader, planeSize);

				} else {

					var compressedSize = reader.ReadInt32BE();
					var compressedPlanes = reader.ReadBytes(compressedSize);
					var dec = new HeatShrinkDecoder(10, 0, 1024);
					var decompressedStream = new MemoryStream();
					dec.Decode(new MemoryStream(compressedPlanes), decompressedStream);
					decompressedStream.Seek(0, SeekOrigin.Begin);
					ReadPlanes(new BinaryReader(decompressedStream), planeSize);
				}
			}
		}

		private void ReadPlanes(BinaryReader reader, int planeSize)
		{
			for (var i = 0; i < BitLength; i++) {
				var marker = reader.ReadByte();
				if (marker == 0x6d) {
					Mask = reader.ReadBytes(planeSize).Select(VniAnimationPlane.Reverse).ToArray(); ;
				} else {
					var plane = new VniAnimationPlane(reader, planeSize, marker);
					Planes.Add(plane);
				}
			}
		}
	}
}
