using System;
using System.Windows.Media;
using LibDmd.Frame;
using NLog;
using WebSocketSharp;

namespace LibDmd.Output.Network
{
	public class NetworkStream : IGray2Destination, IGray4Destination, IColoredGray2Destination, IColoredGray4Destination, IResizableDestination
	{
		public string Name { get; } = "Network Stream";
		public bool IsAvailable { get; private set; }

		private WebSocket _client;
		private Uri _uri;
		private string _gameName;
		private readonly WebsocketSerializer _serializer = new WebsocketSerializer();

		private Color _color = RenderGraph.DefaultColor;
		private Color[] _palette;

		private static NetworkStream _instance;
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public static NetworkStream GetInstance(Uri uri, string romName = null) {
			if (_instance == null) {
				_instance = new NetworkStream();
			}
			_instance.Init(uri, romName);
			return _instance;
		}

		public void Init(Uri uri, string romName = null)
		{
			_uri = uri;
			_gameName = romName;
			Logger.Info("Connecting to WebSocket at {0}", _uri.ToString());
			_client = new WebSocket(_uri.ToString());
			_client.Log.Level = WebSocketSharp.LogLevel.Fatal;
			_client.OnMessage += OnMessage;
			_client.OnError += OnError;
			_client.OnOpen += OnOpen;
			_client.Connect();
			Logger.Info("Connected to Websocket at {0}", _uri.ToString());
		}

		private void OnOpen(object sender, EventArgs e)
		{
			IsAvailable = true;
			Logger.Info("Connected to Websocket at {0}", _uri.ToString());

			if (_gameName != null) {
				_client.Send(_serializer.SerializeGameName(_gameName));
			}
			_client.Send(_serializer.SerializeDimensions(_serializer.Dimensions));
			_client.Send(_serializer.SerializeColor(_color));
			if (_palette != null) {
				_client.Send(_serializer.SerializePalette(_palette));
			}
		}

		private void OnMessage(object sender, MessageEventArgs e)
		{
			Logger.Info("Message from server: " + e.Data);
		}

		private void OnError(object sender, ErrorEventArgs e)
		{
			Logger.Error("Network stream disconnected: " + e.Message);
			IsAvailable = false;
		}

		private void SendGray(DmdFrame frame, int bitlength)
		{
			if (frame.Data.Length < _serializer.Dimensions.Surface) {
				Logger.Info("SendGray: invalid frame received frame.length={0} bitlength={1} dim={2}", frame.Data.Length, bitlength, _serializer.Dimensions);
				return;
			}
			if (IsAvailable) {
				_client.Send(_serializer.SerializeGray(frame.Data, bitlength));
			}

		}

		public void RenderGray2(DmdFrame frame)
		{
			SendGray(frame, 2);
		}

		public void RenderGray4(DmdFrame frame)
		{
			SendGray(frame, 4);
		}

		public void RenderColoredGray2(ColoredFrame frame)
		{
			if (IsAvailable) {
				_client.Send(_serializer.SerializeColoredGray2(frame.Planes, frame.Palette));
			}
		}

		public void RenderColoredGray4(ColoredFrame frame)
		{
			if (IsAvailable) {
				_client.Send(_serializer.SerializeColoredGray4(frame.Planes, frame.Palette));
			}
		}

		public void RenderRgb24(DmdFrame frame)
		{
			if (IsAvailable) {
				_client.Send(_serializer.SerializeRgb24(frame.Data));
			}
		}

		public void SetDimensions(Dimensions dimensions)
		{
			if (IsAvailable) {
				_client.Send(_serializer.SerializeDimensions(dimensions));
			}
		}

		public void SetColor(Color color)
		{
			_color = color;
			if (IsAvailable) {
				_client.Send(_serializer.SerializeColor(color));
			}
		}

		public void SetPalette(Color[] colors, int index = -1)
		{
			_palette = colors;
			if (IsAvailable) {
				_client.Send(_serializer.SerializePalette(colors));
			}
		}

		public void ClearPalette()
		{
			if (IsAvailable) {
				_client.Send(_serializer.SerializeClearPalette());
			}
		}

		public void ClearColor()
		{
			if (IsAvailable) {
				_client.Send(_serializer.SerializeClearColor());
			}
		}

		public void ClearDisplay()
		{
			// ignore
		}

		public void Dispose()
		{
			((IDisposable)_client)?.Dispose();
		}
	}

}
