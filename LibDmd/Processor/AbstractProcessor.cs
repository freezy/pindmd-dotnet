using System;
using System.IO;
using System.Reactive.Subjects;
using System.Windows.Media.Imaging;

namespace LibDmd.Processor
{
	/// <summary>
	/// Base class for all processors that contains common logic.
	/// </summary>
	public abstract class AbstractProcessor
	{
		/// <summary>
		/// Produces frames as they come out of the processor.
		/// Useful for debugging and displaying intermediate results.
		/// </summary>
		public IObservable<BitmapSource> WhenProcessed => _whenProcessed;

		/// <summary>
		/// If set to false, this processor will be ignored by the <see cref="RenderGraph"/>.
		/// </summary>
		public virtual bool Enabled { get; set; } = true;

		/// <summary>
		/// Processes a frame
		/// </summary>
		/// <param name="bmp">Unprocessed frame</param>
		/// <returns>Processed frame</returns>
		public abstract BitmapSource Process(BitmapSource bmp);

		protected Subject<BitmapSource> _whenProcessed = new Subject<BitmapSource>();

		protected static void Dump(BitmapSource image, string filePath)
		{
			using (var fileStream = new FileStream(filePath, FileMode.Create)) {
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(image));
				encoder.Save(fileStream);
			}
		}
	}
}
