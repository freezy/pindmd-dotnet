using System;

namespace LibDmd.Input
{
	public interface IRawSource
	{
		/// <summary>
		/// A display name for the source
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Returns an observable that produces raw data that is sent directly to the display.
		/// </summary>
		IObservable<byte[]> GetRawdata();
	}
}
