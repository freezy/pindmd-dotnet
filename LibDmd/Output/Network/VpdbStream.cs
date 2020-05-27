using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Windows.Media;
using LibDmd.Common;
using LibDmd.Frame;
using Newtonsoft.Json.Linq;
using NLog;
using Quobject.SocketIoClientDotNet.Client;

namespace LibDmd.Output.Network
{
	public class VpdbStream : IGray2Destination, IGray4Destination, IColoredGray2Destination, IColoredGray4Destination, IResizableDestination
	{
		public string Name { get; } = "VPDB Stream";
		public bool IsAvailable { get; } = true;

		public string ApiKey { get; set; }
		public string EndPoint { get; set; } = "https://api-test.vpdb.io/";
		public string AuthUser { get; set; }
		public string AuthPass { get; set; }

		private readonly Socket _socket;
		private bool _connected;
		private bool _streaming;

		private Dimensions _dimensions;
		private Color _color = RenderGraph.DefaultColor;
		private Color[] _palette;

		private readonly long _startedAt = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		private JObject Welcome => new JObject {
			{ "width", _dimensions.Width },
			{ "height", _dimensions.Height },
			{ "color", ColorUtil.ToInt(_color) },
			{ "palette", new JArray(ColorUtil.ToIntArray(_palette)) }
		};

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VpdbStream()
		{
			Logger.Info("Connecting to VPDB...");
			_socket = IO.Socket(EndPoint);
			_socket.On(Socket.EVENT_CONNECT, () => {
				_connected = true;
				Logger.Info("Connected to VPDB.");
				_socket.Emit("produce", Welcome);
			});
			_socket.On(Socket.EVENT_RECONNECT, () => {
				_connected = true;
				Logger.Info("Reconnected to VPDB.");
				_socket.Emit("produce", Welcome);
			});
			_socket.On(Socket.EVENT_DISCONNECT, () => {
				_connected = false;
				Logger.Info("Disconnected from VPDB.");
			});
			_socket.On(Socket.EVENT_MESSAGE, data => {
				Logger.Info("Streaming message: {0}", data);
			});
			_socket.On(Socket.EVENT_DISCONNECT, data => {
				Logger.Info("Stream disconnected.");
			});
			_socket.On(Socket.EVENT_CONNECT_ERROR, data => {
				Logger.Warn("Streaming connection error: {0}", data);
			});
			_socket.On(Socket.EVENT_CONNECT_TIMEOUT, data => {
				Logger.Info("Streaming connection timeout.", data);
			});
			_socket.On(Socket.EVENT_ERROR, data => {
				Logger.Warn("Streaming error: {0}", data);
			});
			_socket.On(Socket.EVENT_RECONNECTING, data => {
				Logger.Info("Streaming reconnecting..");
			});
			_socket.On(Socket.EVENT_RECONNECT_ERROR, data => {
				Logger.Info("Streaming reconnection error.");
			});
			_socket.On(Socket.EVENT_RECONNECT_ATTEMPT, data => {
				Logger.Info("Streaming reconnection attempt {0}.", data);
			});
			_socket.On(Socket.EVENT_RECONNECT_FAILED, data => {
				Logger.Warn("Streaming reconnection failed.");
			});

			_socket.On("resume", () => {
				_streaming = true;
				Logger.Info("Resuming streaming frames...");
			});
			_socket.On("pause", () => {
				_streaming = false;
				Logger.Info("Pausing streaming frames...");
			});
		}

		public void SetDimensions(Dimensions dimensions)
		{
			_dimensions = dimensions;
			EmitObject("dimensions", new JObject { { "width", _dimensions.Width }, { "height", _dimensions.Height } });
		}

		public void RenderGray2(DmdFrame frame)
		{
			EmitTimestampedData("gray2planes",
				frame.Data.Length / 4,
				(data, offset) => FrameUtil.Copy(FrameUtil.Split(_dimensions, 2, frame.Data), data, offset
			));
		}

		public void RenderGray4(DmdFrame frame)
		{
			EmitTimestampedData("gray4planes",
				frame.Data.Length / 2,
				(data, offset) => FrameUtil.Copy(FrameUtil.Split(_dimensions, 4, frame.Data), data, offset
			));
		}

		public void RenderColoredGray2(ColoredFrame frame)
		{
			if (frame.Planes.Length == 0) {
				return;
			}
			const int numColors = 4;
			const int bytesPerColor = 3;
			var dataLength = bytesPerColor * numColors + frame.Planes[0].Length * frame.Planes.Length;
			EmitTimestampedData("coloredgray2", dataLength, (data, offset) => {
				Buffer.BlockCopy(ColorUtil.ToByteArray(frame.Palette), 0, data, offset, bytesPerColor * numColors);
				FrameUtil.Copy(frame.Planes, data, offset + bytesPerColor * numColors);
			});
		}

		public void RenderColoredGray4(ColoredFrame frame)
		{
			if (frame.Planes.Length == 0) {
				return;
			}
			const int numColors = 16;
			const int bytesPerColor = 3;
			var dataLength = bytesPerColor * numColors + frame.Planes[0].Length * frame.Planes.Length;
			EmitTimestampedData("coloredgray4", dataLength, (data, offset) => {
				Buffer.BlockCopy(ColorUtil.ToByteArray(frame.Palette), 0, data, offset, bytesPerColor * numColors);
				FrameUtil.Copy(frame.Planes, data, offset + bytesPerColor * numColors);
			});
		}

		public void RenderRgb24(DmdFrame frame)
		{
			EmitTimestampedData("rgb24frame",
				frame.Data.Length,
				(data, offset) => Buffer.BlockCopy(frame.Data, 0, data, offset, frame.Data.Length
			));
		}

		public void SetColor(Color color)
		{
			_color = color;
			EmitObject("color", new JObject { { "color", ColorUtil.ToInt(color) } });
		}

		public void SetPalette(Color[] colors , int index = -1)
		{
			_palette = colors;
			EmitObject("palette", new JObject { { "palette", new JArray(ColorUtil.ToIntArray(colors)) } });
		}

		public void ClearPalette()
		{
			EmitData("clearPalette");
		}

		public void ClearColor()
		{
			EmitData("clearColor");
		}

		public void ClearDisplay()
		{
			EmitData("clearDisplay");
		}

		public void Dispose()
		{
			_socket?.Emit("stop");
			_socket?.Close();
		}

		/// <summary>
		/// Adds a timestamp to a byte array and sends it to the socket.
		/// </summary>
		/// <param name="eventName">Name of the event</param>
		/// <param name="dataLength">Length of the payload to send (without time stamp)</param>
		/// <param name="copy">Function that copies data to the provided array. Input: array with 8 bytes of timestamp and dataLength bytes to write</param>
		private void EmitTimestampedData(string eventName, int dataLength, Action<byte[], int> copy)
		{
			if (!_connected || !_streaming) {
				return;
			}
			try {
				var timestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				var data = new byte[dataLength + 8];
				Buffer.BlockCopy(BitConverter.GetBytes(timestamp - _startedAt), 0, data, 0, 8);
				copy(data, 8);
				_socket.Emit(eventName, data);

			} catch (Exception e) {
				Logger.Error(e, "Error sending " + eventName + " to socket.");
				_connected = false;
			}
		}

		private void EmitObject(string eventName, JObject data)
		{
			if (!_connected || !_streaming) {
				return;
			}
			try {
				_socket.Emit(eventName, data);
			} catch (Exception e) {
				Logger.Error(e, "Error sending " + eventName + " to socket.");
				_connected = false;
			}
		}

		private void EmitData(string eventName, IEnumerable data = null)
		{
			if (!_connected || !_streaming) {
				return;
			}
			try {
				if (data == null) {
					_socket.Emit(eventName);
				} else {
					_socket.Emit(eventName, data);
				}
			} catch (Exception e) {
				Logger.Error(e, "Error sending " + data + " to socket.");
				_connected = false;
			}
		}

		public static byte[] Compress(byte[] raw)
		{
			using (var memory = new MemoryStream()) {
				using (var gzip = new GZipStream(memory, CompressionMode.Compress, true)) { gzip.Write(raw, 0, raw.Length);
				}
				return memory.ToArray();
			}
		}
	}
}
