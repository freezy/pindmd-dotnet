using LibDmd.Frame;

namespace LibDmd.Output
{
	/// <summary>
	/// Output devices implementing this interface are able to
	/// render 2-bit frames as colored bit planes.
	/// </summary>
	public interface IColoredGray2Destination : IRgb24Destination
	{
		/// <summary>
		/// Renders a colored frame in 2 bits.
		/// </summary>
		/// <param name="frame">Frame to render</param>
		void RenderColoredGray2(ColoredFrame frame);
	}
}
