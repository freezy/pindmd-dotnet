using System.Windows.Media;
using LibDmd.Frame;

namespace LibDmd.Output
{
	/// <summary>
	/// Output devices implementing this interface are able to
	/// render RGB 24-bit frames.
	/// </summary>
	public interface IRgb24Destination : IDestination
	{

		/// <summary>
		/// Renders a frame in 24 bit RGB.
		/// </summary>
		/// <param name="frame">Array containing Width * Height * 3 bytes, with RGB values between 0 and 255 for every pixel.</param>
		void RenderRgb24(DmdFrame frame);

		/// <summary>
		/// Sets the color with which a grayscale source is rendered on the RGB display.
		/// </summary>
		/// <param name="color">Rendered color</param>
		void SetColor(Color color);

		/// <summary>
		/// Sets the palette for rendering grayscale images.
		/// </summary>
		/// <param name="colors"></param>
		void SetPalette(Color[] colors, int index = -1);

		/// <summary>
		/// Removes a previously set palette
		/// </summary>
		void ClearPalette();

		/// <summary>
		/// Resets the color
		/// </summary>
		void ClearColor();
	}
}
