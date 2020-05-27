using LibDmd.Frame;

namespace LibDmd.Output
{
	/// <summary>
	/// Output devices implementing this interface are able to
	/// render 4-bit frames as colored bit planes.
	/// </summary>
	public interface IColoredGray4Destination : IRgb24Destination
	{
		/// <summary>
		/// Renders a colored frame in 4 bits.
		/// </summary>
		/// <param name="frame">Frame to render</param>
		void RenderColoredGray4(ColoredFrame frame);
	}
}
