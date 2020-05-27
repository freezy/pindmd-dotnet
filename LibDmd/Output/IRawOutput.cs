using System;

namespace LibDmd.Output
{
	/// <summary>
	/// Output devices implementing this interface can receive raw
	/// data that is sent directly to the device.
	/// </summary>
	/// <remarks>
	/// Note that raw data is device-specific.
	/// </remarks>
	public interface IRawOutput : IDisposable
	{
		/// <summary>
		/// Sends raw data to the device.
		/// </summary>
		/// <param name="data"></param>
		void RenderRaw(byte[] data);
	}
}
