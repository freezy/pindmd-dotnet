using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using LibDmd.Common;

namespace LibDmd.Converter.Colorize
{
	public class VniAnimation : Animation
	{
		public VniAnimation(BinaryReader reader, int fileVersion) : base(reader.BaseStream.Position)
		{
			// animations name
			var nameLength = reader.ReadInt16BE();
			Name = nameLength > 0 ? Encoding.UTF8.GetString(reader.ReadBytes(nameLength)) : "<undefined>";

			// other data
			Cycles = reader.ReadInt16BE();
			Hold = reader.ReadInt16BE();
			ClockFrom = reader.ReadInt16BE();
			ClockSmall = reader.ReadByte() != 0;
			ClockInFront = reader.ReadByte() != 0;
			ClockOffsetX = reader.ReadInt16BE();
			ClockOffsetY = reader.ReadInt16BE();
			RefreshDelay = reader.ReadInt16BE();
			Type = reader.ReadByte();
			Fsk = reader.ReadByte();

			int numFrames = reader.ReadInt16BE();
			if (numFrames < 0) {
				numFrames += 65536;
			}

			if (fileVersion >= 2) {
				ReadPalettesAndColors(reader);
			}
			if (fileVersion >= 3) {
				EditMode = (AnimationEditMode)reader.ReadByte();
			}
			if (fileVersion >= 4) {
				Width = reader.ReadInt16BE();
				Height = reader.ReadInt16BE();
			}
			if (fileVersion >= 5)
			{
				int numMasks = reader.ReadInt16BE();
				Masks = new byte[numMasks][];
				for (var i = 0; i < numMasks; i++)
				{
					int locked = reader.ReadByte();
					int size = reader.ReadInt16BE();
					Masks[i] = reader.ReadBytesRequired(size).Select(VniAnimationPlane.Reverse).ToArray();
				}
			}

			if (fileVersion >= 6)
			{
				int LinkedAnimation = reader.ReadByte();
				int size = reader.ReadInt16BE();
				string AnimName = size > 0 ? Encoding.UTF8.GetString(reader.ReadBytes(nameLength)) : "<undefined>";
				uint startFrame = reader.ReadUInt32BE();
			}

			Logger.Debug("VNI[{3}] Reading {0} frame{1} for animation \"{2}\"...", numFrames, numFrames == 1 ? "" : "s", Name, reader.BaseStream.Position);
			Frames = new AnimationFrame[numFrames];
			AnimationDuration = 0;
			for (var i = 0; i < numFrames; i++) {
				Frames[i] = new VniAnimationFrame(reader, fileVersion, AnimationDuration);
				if (Frames[i].Mask != null && TransitionFrom == 0) {
					TransitionFrom = i;
				}
				AnimationDuration += Frames[i].Delay;
			}
		}

		private void ReadPalettesAndColors(BinaryReader reader)
		{
			PaletteIndex = reader.ReadInt16BE();
			var numColors = reader.ReadInt16BE();
			if (numColors <= 0) {
				return;
			}
			AnimationColors = new Color[numColors];
			for (var i = 0; i < numColors; i++) {
				AnimationColors[i] = Color.FromRgb(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
			}
			Logger.Debug("VNI[{2}] Found {0} colors for palette {1}.", numColors, PaletteIndex, reader.BaseStream.Position);
		}

		public override string ToString()
		{
			return $"{Name}, {Frames.Length} frames";
		}
	}
}
