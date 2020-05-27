using System;

namespace LibDmd
{
	public interface IRenderer : IDisposable
	{
		/// <summary>
		/// Run before <see cref="StartRendering"/>
		/// </summary>
		/// <remarks>
		/// Either that or <see cref="RenderGraphCollection.Init"/> must be run.
		/// </remarks>
		/// <returns>This instance</returns>
		IRenderer Init();

		/// <summary>
		/// Subscribes to the source and hence starts receiving and processing data
		/// as soon as the source produces them.
		/// </summary>
		/// <param name="onCompleted">When the source stopped producing frames.</param>
		/// <param name="onError">When a known error occurs.</param>
		/// <returns>An IDisposable that stops rendering when disposed.</returns>
		IDisposable StartRendering(Action onCompleted, Action<Exception> onError = null);

		/// <summary>
		/// Clears the displays on all destinations.
		/// </summary>
		void ClearDisplay();
	}
}
