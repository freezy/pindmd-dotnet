using LibDmd.Frame;

namespace LibDmd.Output
{
	/// <summary>
	/// Output devices implementing this interface are able to
	/// render alphanumeric frames from non-DMD sources.
	/// </summary>
	public interface IAlphaNumericDestination : IDestination
	{
		/// <summary>
		/// Renders an alphanumeric frame.
		/// </summary>
		/// <param name="frame">Frame to render</param>
		void RenderAlphaNumeric(AlphaNumericFrame frame);
	}
}
