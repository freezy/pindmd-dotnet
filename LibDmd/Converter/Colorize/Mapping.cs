using System.IO;
using LibDmd.Common;
using NLog;

namespace LibDmd.Converter.Colorize
{
	/// <summary>
	/// S Mäpping definiärt Häsches wo Sachä uisleesid wiä ä Palettäwächsu odr
	/// än Animazion.
	/// </summary>
	public class Mapping
	{
		/// <summary>
		/// Wefu Bytes äs Mäpping bruicht
		/// </summary>
		public static readonly int Length = 11;

		/// <summary>
		/// D Checkum vom Biud
		/// </summary>
		public readonly uint Checksum;

		/// <summary>
		/// Dr Modus. 
		/// </summary>
		/// 
		/// <remarks>
		/// Dr Modus beschribt was genai passiert und wiäd Animazion faus gladä
		/// aagwändet wird.
		/// </remarks>
		public readonly SwitchMode Mode;

		/// <summary>
		/// Dr Palettäindex
		/// </summary>
		public readonly ushort PaletteIndex;

		/// <summary>
		/// Wiä lang's gaht bis mr zrugg zur Standard-Palettä wächslet (wenn 0 gar nid zrugg wächslä).
		/// </summary>
		public readonly uint Duration;

		/// <summary>
		/// D Byte-Position vodr Animazion im FSQ/VNI-Feil.
		/// </summary>
		public readonly uint Offset;

		/// <summary>
		/// Wenn ja, de tiämmer än Animazion uisäsuächä und loslah, sisch ischs numä ä
		/// Palettäwächsü oder sisch ä Seich.
		/// </summary>
		public bool IsAnimation => Mode != SwitchMode.Event && Mode != SwitchMode.Palette;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public Mapping(BinaryReader reader)
		{
			Checksum = reader.ReadUInt32BE();
			//Logger.Trace("  [{1}] [palette] Read checksum as {0}", Checksum, reader.BaseStream.Position);
			Mode = (SwitchMode)reader.ReadByte();
			//Logger.Trace("  [{1}] [palette] Read mode as {0}", Mode, reader.BaseStream.Position);
			PaletteIndex = reader.ReadUInt16BE();
			//Logger.Trace("  [{1}] [palette] Read index as {0}", PaletteIndex, reader.BaseStream.Position);
			if (Mode == SwitchMode.Palette) {
				Duration = reader.ReadUInt32BE();
				//Logger.Trace("  [{1}] [palette] Read duration as {0}", Duration, reader.BaseStream.Position);
			} else {
				Offset = reader.ReadUInt32BE();
				//Logger.Trace("  [{1}] [palette] Read offset as {0}", Offset, reader.BaseStream.Position);
			}
		}
	}
}
