using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using LibDmd.Common;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Output.FileOutput
{
	public class GifOutput : IBitmapDestination
	{
		public string Name { get; } = "GIF Writer";
		public bool IsAvailable { get; } = true;

		private readonly GifWriter _outputGif;

		private int _lastTick;
		private BitmapSource _lastBitmap;
		private bool _disposed = false;

		public GifOutput(string path)
		{
			if (Path.GetExtension(path.ToLower()).Equals(".gif")) {
				if (!Directory.Exists(Path.GetDirectoryName(path))) {
					throw new InvalidFolderException($"Cannot write to {path}, because that folder does not exist.");
				}
				_outputGif = new GifWriter(path, 40, 0);

			} else {
				throw new ArgumentException("Path must point to a .gif file.");
			}
		}

		/// <summary>
		/// Renders an image to the display.
		/// </summary>
		/// <param name="bmp">Any bitmap</param>
		public void RenderBitmap(BmpFrame bmp)
		{
			// since we don't know the frame length before the next frame, we
			// write the last frame and the current frame when disposing.
			if (_lastBitmap != null) {
				_outputGif.WriteFrame(ImageUtil.ConvertToImage(_lastBitmap), Environment.TickCount - _lastTick);
			}

			_lastBitmap = bmp.Bitmap;
			_lastTick = Environment.TickCount;
		}

		public void Dispose()
		{
			if (_disposed) {
				return;
			}
			_disposed = true;
			if (_lastBitmap != null) {
				_outputGif.WriteFrame(ImageUtil.ConvertToImage(_lastBitmap), Environment.TickCount - _lastTick);
			}
			_outputGif.Dispose();
		}

		public void ClearDisplay()
		{
			// no, we don't write a blank image.
		}
	}

	/// <summary>
	/// A class capable of generating animated GIFs.
	/// </summary>
	/// <remarks>
	/// See <a href="https://stackoverflow.com/questions/1196322/how-to-create-an-animated-gif-in-net#answer-32810041">Original code at SO</a>
	/// </remarks>
	public class GifWriter : IDisposable
	{
		#region Properties
		/// <summary>
		/// Gets or Sets the Default Width of a Frame. Used when unspecified.
		/// </summary>
		public int DefaultWidth { get; set; }

		/// <summary>
		/// Gets or Sets the Default Height of a Frame. Used when unspecified.
		/// </summary>
		public int DefaultHeight { get; set; }

		/// <summary>
		/// Gets or Sets the Default Delay in Milliseconds.
		/// </summary>
		public int DefaultFrameDelay { get; set; }

		/// <summary>
		/// The Number of Times the Animation must repeat.
		/// -1 indicates no repeat. 0 indicates repeat indefinitely
		/// </summary>
		public int Repeat { get; }
		#endregion

		#region Fields
		private const long SourceGlobalColorInfoPosition = 10;
		private const long SourceImageBlockPosition = 789;

		readonly BinaryWriter _writer;
		bool _firstFrame = true;
		readonly object _syncLock = new object();
		#endregion

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Creates a new instance of GifWriter.
		/// </summary>
		/// <param name="outStream">The <see cref="Stream"/> to output the Gif to.</param>
		/// <param name="defaultFrameDelay">Default Delay between consecutive frames... FrameRate = 1000 / DefaultFrameDelay.</param>
		/// <param name="repeat">No of times the Gif should repeat... -1 to repeat indefinitely.</param>
		public GifWriter(Stream outStream, int defaultFrameDelay = 500, int repeat = -1)
		{
			if (outStream == null) {
				throw new ArgumentNullException(nameof(outStream));
			}

			if (defaultFrameDelay <= 0) {
				throw new ArgumentOutOfRangeException(nameof(defaultFrameDelay));
			}

			if (repeat < -1) {
				throw new ArgumentOutOfRangeException(nameof(repeat));
			}

			_writer = new BinaryWriter(outStream);
			DefaultFrameDelay = defaultFrameDelay;
			Repeat = repeat;
		}

		/// <summary>
		/// Creates a new instance of GifWriter.
		/// </summary>
		/// <param name="fileName">The path to the file to output the Gif to.</param>
		/// <param name="defaultFrameDelay">Default Delay between consecutive frames... FrameRate = 1000 / DefaultFrameDelay.</param>
		/// <param name="repeat">No of times the Gif should repeat... -1 to repeat indefinitely.</param>
		public GifWriter(string fileName, int defaultFrameDelay = 500, int repeat = -1)
			: this(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read), defaultFrameDelay, repeat)
		{
		}

		/// <summary>
		/// Adds a frame to this animation.
		/// </summary>
		/// <param name="image">The image to add</param>
		/// <param name="delay">Delay in Milliseconds between this and last frame... 0 = <see cref="DefaultFrameDelay"/></param>
		public void WriteFrame(Image image, int delay = 0)
		{
			lock (_syncLock) {
				using (var gifStream = new MemoryStream()) {
					image.Save(gifStream, ImageFormat.Gif);

					// Steal the global color table info
					if (_firstFrame) {
						InitHeader(gifStream, _writer, image.Width, image.Height);
					}

					WriteGraphicControlBlock(gifStream, _writer, delay == 0 ? DefaultFrameDelay : delay);
					WriteImageBlock(gifStream, _writer, !_firstFrame, 0, 0, image.Width, image.Height);
				}
			}
			if (_firstFrame) {
				_firstFrame = false;
			}
		}

		#region Write
		private void InitHeader(Stream sourceGif, BinaryWriter writer, int width, int height)
		{
			// File Header
			writer.Write("GIF".ToCharArray()); // File type
			writer.Write("89a".ToCharArray()); // File Version

			writer.Write((short)(DefaultWidth == 0 ? width : DefaultWidth));    // Initial Logical Width
			writer.Write((short)(DefaultHeight == 0 ? height : DefaultHeight)); // Initial Logical Height

			sourceGif.Position = SourceGlobalColorInfoPosition;
			writer.Write((byte)sourceGif.ReadByte()); // Global Color Table Info
			writer.Write((byte)0);                    // Background Color Index
			writer.Write((byte)0);                    // Pixel aspect ratio
			WriteColorTable(sourceGif, writer);

			// App Extension Header for Repeating
			if (Repeat == -1) {
				return;
			}

			writer.Write(unchecked((short)0xff21)); // Application Extension Block Identifier
			writer.Write((byte)0x0b);                  // Application Block Size
			writer.Write("NETSCAPE2.0".ToCharArray()); // Application Identifier
			writer.Write((byte)3);                     // Application block length
			writer.Write((byte)1);
			writer.Write((short)Repeat);               // Repeat count for images.
			writer.Write((byte)0);                     // terminator
		}

		static void WriteColorTable(Stream sourceGif, BinaryWriter writer)
		{
			// Locating the image color table
			sourceGif.Position = 13;
			var colorTable = new byte[768];
			sourceGif.Read(colorTable, 0, colorTable.Length);
			writer.Write(colorTable, 0, colorTable.Length);
		}

		static void WriteGraphicControlBlock(Stream sourceGif, BinaryWriter writer, int frameDelay)
		{
			sourceGif.Position = 781;                       // Locating the source GCE
			var blockhead = new byte[8];
			sourceGif.Read(blockhead, 0, blockhead.Length); // Reading source GCE

			writer.Write(unchecked((short)0xf921));           // Identifier
			writer.Write((byte)0x04);                         // Block Size
			writer.Write((byte)(blockhead[3] & 0xf7 | 0x08)); // Setting disposal flag
			writer.Write((short)(frameDelay / 10));           // Setting frame delay
			writer.Write(blockhead[6]);                       // Transparent color index
			writer.Write((byte)0);                            // Terminator
		}

		static void WriteImageBlock(Stream sourceGif, BinaryWriter writer, bool includeColorTable, int x, int y, int width, int height)
		{
			sourceGif.Position = SourceImageBlockPosition; // Locating the image block
			var header = new byte[11];
			sourceGif.Read(header, 0, header.Length);
			writer.Write(header[0]);     // Separator
			writer.Write((short)x);      // Position X
			writer.Write((short)y);      // Position Y
			writer.Write((short)width);  // Width
			writer.Write((short)height); // Height

			// If first frame, use global color table - else use local
			if (includeColorTable) {
				sourceGif.Position = SourceGlobalColorInfoPosition;
				// Enabling local color table
				writer.Write((byte)(sourceGif.ReadByte() & 0x3f | 0x80));
				WriteColorTable(sourceGif, writer);
			} else {
				// Disabling local color table
				writer.Write((byte)(header[9] & 0x07 | 0x07));
			}

			// LZW Min Code Size
			writer.Write(header[10]);

			// Read/Write image data
			sourceGif.Position = SourceImageBlockPosition + header.Length;

			var dataLength = sourceGif.ReadByte();
			while (dataLength > 0) {
				var imgData = new byte[dataLength];
				sourceGif.Read(imgData, 0, dataLength);

				writer.Write((byte)dataLength);
				writer.Write(imgData, 0, dataLength);
				dataLength = sourceGif.ReadByte();
			}

			// Terminator
			writer.Write((byte)0);
		}
		#endregion

		/// <summary>
		/// Frees all resources used by this object.
		/// </summary>
		public void Dispose()
		{
			Logger.Debug("Closing GIF writer...");

			// Complete File
			_writer.Write((byte)0x3b); // File Trailer

			_writer.BaseStream.Dispose();
			_writer.Dispose();
		}
	}
}
