using LibDmd.Frame;

namespace LibDmd.Output
{
	/// <summary>
	/// Indicates that an output device with multiple sizes.
	/// </summary>
	///
	/// <remarks>
	/// By "multiple sizes", we mean that the physical display is fixed, but
	/// it can receive multiple resolution formats.
	/// </remarks>
	public interface IMultiSizeDestination
	{
		/// <summary>
		/// Supported dimensions
		/// </summary>
		Dimensions[] Sizes { get; }
	}
}
