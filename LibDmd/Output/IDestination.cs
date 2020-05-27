using System;

namespace LibDmd.Output
{
	/// <summary>
	/// A destination where frames are rendered, like a physical display or
	/// some sort of virtual DMD.
	/// </summary>
	/// <remarks>
	/// When implementing a destination, make sure to only implement the "native"
	/// bit lengths. Convertion if necessary is done in the render graph directly.
	/// </remarks>
	public interface IDestination : IDisposable
	{
		/// <summary>
		/// A human-friendly name for the device
		/// </summary>
		string Name { get; }

		/// <summary>
		/// If true, destination is available and can be used as target.
		/// </summary>
		bool IsAvailable { get; }

		/// <summary>
		/// Clears the display.
		/// </summary>
		void ClearDisplay();
	}

	/// <summary>
	/// Thrown on operations that don't make sense without the display connected.
	/// </summary>
	/// <seealso cref="IDestination.IsAvailable"/>
	public class SourceNotAvailableException : Exception
	{
	}

	public class UnsupportedResolutionException : Exception
	{
		public UnsupportedResolutionException(string message) : base(message)
		{
		}
	}

	public class RenderException : Exception
	{
		public RenderException(string message) : base(message)
		{
		}
	}
}
