using System;
using LibDmd.Frame;

namespace LibDmd.Input
{

	/// <summary>
	/// A source that is able to deliver 2-bit frames without conversion.
	/// </summary>
	public interface IGray2Source : ISource
	{
		/// <summary>
		/// Returns an observable that produces a sequence of 2-bit frames.
		/// 
		/// The returned byte array contains Width * Height bytes, with values 
		/// between 0 and 3 for every pixel.
		/// </summary>
		/// <remarks>When disposed, frame production must stop.</remarks>
		IObservable<DmdFrame> GetGray2Frames();
	}
}
