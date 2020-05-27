using System;
using LibDmd.Frame;

namespace LibDmd.Input
{
	/// <summary>
	/// A source that is able to deliver color-encoded bit planes for two bits 
	/// per pixel.
	/// </summary>
	public interface IColoredGray2Source : ISource
	{
		/// <summary>
		/// Returns an observable that produces a sequence of 2-bit sub frames
		/// with a 4-color palette.
		/// </summary>
		/// <remarks>When disposed, frame production must stop.</remarks>
		IObservable<ColoredFrame> GetColoredGray2Frames();
	}
}
