using System;
using LibDmd.Frame;

namespace LibDmd.Input
{

	/// <summary>
	/// A source that is able to deliver 4-bit frames without conversion.
	/// </summary>
	public interface IGray4Source : ISource
	{
		/// <summary>
		/// Returns an observable that produces a sequence of 4-bit frames.
		/// 
		/// The returned byte array contains Width * Height bytes, with values 
		/// between 0 and 15 for every pixel.
		/// </summary>
		/// <remarks>When disposed, frame production must stop.</remarks>
		IObservable<DmdFrame> GetGray4Frames();
	}
}
