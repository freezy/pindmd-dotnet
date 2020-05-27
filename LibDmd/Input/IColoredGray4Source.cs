using System;
using LibDmd.Frame;

namespace LibDmd.Input
{
	/// <summary>
	/// A source that is able to deliver color-encoded bit planes for four bits
	/// per pixel.
	/// </summary>
	public interface IColoredGray4Source : ISource
	{
		/// <summary>
		/// Returns an observable that produces a sequence of 4-bit sub frames
		/// with a 16-color palette.
		/// </summary>
		/// <remarks>When disposed, frame production must stop.</remarks>
		IObservable<ColoredFrame> GetColoredGray4Frames();
	}
}
