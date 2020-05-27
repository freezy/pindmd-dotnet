using LibDmd.Frame;

namespace LibDmd.Converter
{
	/// <summary>
	/// Converts a frame from a given bit length to another bit length.
	/// </summary>
	public interface IConverter
	{
		/// <summary>
		/// Source bit length
		/// </summary>
		FrameFormat From { get; }

		/// <summary>
		/// Receives frames and outputs them to the output sources the converter implements.
		/// </summary>
		///
		/// <remarks>
		/// Note that if your convertor doesn't implement any ISource interface,
		/// frames will just be dropped.
		///
		/// If this method doesn't send anything to its output sources, the frame is
		/// equally dropped.
		/// </remarks>
		///
		/// <param name="frame">Source frame, as top-left to bottom-right pixel array</param>
		void Convert(DmdFrame frame);

		/// <summary>
		/// Initializes the converter. Run before rendering is started and after
		/// Dimensions have been initialized.
		/// </summary>
		/// <returns></returns>
		void Init();

		/// <summary>
		/// Must be run when dimensions of the source change.
		/// </summary>
		/// <param name="dim">New dimensions of the source</param>
		void SetDimensions(Dimensions dim);
	}
}
