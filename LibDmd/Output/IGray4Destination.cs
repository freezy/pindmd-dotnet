using LibDmd.Frame;

namespace LibDmd.Output
{
	/// <summary>
	/// Output devices implementing this interface are able to
	/// render 4 bit frames.
	/// </summary>
	public interface IGray4Destination : IDestination
	{
		/// <summary>
		/// Renders a frame in 4 bit.
		/// </summary>
		/// <param name="frame">Array containing Width * Height bytes, with values between 0 and 15 for every pixel.</param>
		void RenderGray4(DmdFrame frame);
	}
}
