using System;
using LibDmd.Frame;

namespace LibDmd.Input
{
	/// <summary>
	/// A source that produces bitmaps.
	/// </summary>
	public interface IBitmapSource : ISource
	{
		/// <summary>
		/// Returns an observable that produces a sequence of frames.
		/// </summary>
		/// <remarks>When disposed, frame production must stop.</remarks>
		IObservable<BmpFrame> GetBitmapFrames();
	}
}
