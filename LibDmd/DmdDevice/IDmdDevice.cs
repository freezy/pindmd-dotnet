using System.Windows.Media;
using LibDmd.Frame;

namespace LibDmd.DmdDevice
{
	public interface IDmdDevice
	{
		void Close();
		void SetColorize(bool colorize);
		void SetGameName(string gameName);
		void SetColor(Color color);
		void Init();
		void LoadPalette(uint palIndex);
		void SetPalette(Color[] colors);
		void RenderRgb24(DmdFrame frame);
		void RenderGray4(DmdFrame frame);
		void RenderGray2(DmdFrame frame);
		void RenderAlphaNumeric(NumericalLayout numericalLayout, ushort[] readUInt16Array, ushort[] ushorts);
	}
}
