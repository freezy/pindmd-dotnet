using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media;
using LibDmd.Frame;
using MimeTypes;
using NLog;
using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using HttpStatusCode = WebSocketSharp.Net.HttpStatusCode;

namespace LibDmd.Output.Network
{
	public class BrowserStream : IGray2Destination, IGray4Destination, IColoredGray2Destination, IColoredGray4Destination, IResizableDestination
	{
		public string Name { get; } = "Browser Stream";
		public bool IsAvailable { get; } = true;

		private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
		private readonly Dictionary<string, string> _www = new Dictionary<string, string>();
		private readonly HttpServer _server;
		private readonly List<DmdSocket>  _sockets = new List<DmdSocket>();
		private readonly string _gameName;

		private Dimensions _dimensions;
		private Color _color = RenderGraph.DefaultColor;
		private Color[] _palette;

		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public BrowserStream(int port, string romName = null)
		{
			// map embedded www resources to _www
			const string prefix = "LibDmd.Output.Network.www.";
			_assembly.GetManifestResourceNames()
				.Where(res => res.StartsWith(prefix))
				.ToList()
				.ForEach(res => _www["/" + res.Substring(prefix.Length)] = res);
			_www["/"] = prefix + "index.html";

			_gameName = romName;

			_server = new HttpServer(port);
			_server.OnGet += (sender, e) => {

				var req = e.Request;
				var res = e.Response;

				var path = req.RawUrl;
				var output = res.OutputStream;

				if (_www.ContainsKey(path)) {
					res.StatusCode = (int)HttpStatusCode.OK;
					res.ContentType = GetMimeType(Path.GetExtension(path));
					res.ContentEncoding = Encoding.UTF8;
					using (var input = _assembly.GetManifestResourceStream(_www[path])) {
						res.ContentLength64 = input.Length;
						CopyStream(input, output);
					}
				} else {
					Logger.Warn("Path {0} not found in assembly.", path);
					res.StatusCode = (int)HttpStatusCode.NotFound;
				}
			};
			_server.AddWebSocketService("/dmd", () => {
				var socket = new DmdSocket(this);
				_sockets.Add(socket);
				return socket;
			});
			_server.Start();
			if (_server.IsListening) {
				Logger.Info("Listening on port {0}, and providing WebSocket services: [ {1} ]", _server.Port, string.Join(", ", _server.WebSocketServices.Paths));
			}
		}

		public void Init(DmdSocket socket)
		{
			Logger.Debug("Init socket");
			if (_gameName != null)
			{
				socket.SendGameName(_gameName);
			}
			socket.SendDimensions(_dimensions);
			socket.SendColor(_color);
			if (_palette != null) {
				socket.SendPalette(_palette);
			}
		}

		public void RenderGray2(DmdFrame frame)
		{
			_sockets.ForEach(s => s.SendGray(frame.Data, 2));
		}

		public void RenderGray4(DmdFrame frame)
		{
			_sockets.ForEach(s => s.SendGray(frame.Data, 4));
		}

		public void RenderColoredGray2(ColoredFrame frame)
		{
			_sockets.ForEach(s => s.SendColoredGray2(frame.Planes, frame.Palette));
		}

		public void RenderColoredGray4(ColoredFrame frame)
		{
			_sockets.ForEach(s => s.SendColoredGray4(frame.Planes, frame.Palette));
		}

		public void RenderRgb24(DmdFrame frame)
		{
			_sockets.ForEach(s => s.SendRgb24(frame.Data));
		}

		public void SetDimensions(Dimensions dimensions)
		{
			_dimensions = dimensions;
			_sockets.ForEach(s => s.SendDimensions(dimensions));
		}

		public void SetColor(Color color)
		{
			_color = color;
			_sockets.ForEach(s => s.SendColor(color));
		}

		public void SetPalette(Color[] colors, int index = -1)
		{
			_palette = colors;
			_sockets.ForEach(s => s.SendPalette(colors));
		}

		public void ClearPalette()
		{
			_sockets.ForEach(s => s.SendClearPalette());
		}

		public void ClearColor()
		{
			_sockets.ForEach(s => s.SendClearColor());
		}

		public void Closed(DmdSocket socket)
		{
			_sockets.Remove(socket);
			Logger.Debug("Socket closed");
		}

		public void ClearDisplay()
		{
			// ignore
		}

		public void Dispose()
		{
			_server.Stop();
		}

		private static string GetMimeType(string ext)
		{
			return string.IsNullOrEmpty(ext) ? "text/html" : MimeTypeMap.GetMimeType(ext);
		}

		private static void CopyStream(Stream input, Stream output)
		{
			// Insert null checking here for production
			var buffer = new byte[8192];

			int bytesRead;
			while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0) {
				output.Write(buffer, 0, bytesRead);
			}
		}

	}

	public class DmdSocket : WebSocketBehavior
	{
		private readonly BrowserStream _dest;
		private readonly WebsocketSerializer _serializer;

		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public DmdSocket(BrowserStream dest)
		{
			_dest = dest;
			_serializer = new WebsocketSerializer();
		}

		public void SendGray(byte[] frame, int bitlength)
		{
			if (frame.Length < _serializer.Dimensions.Surface) {
				Logger.Info("SendGray: invalid frame received frame.length={0} bitlength={1} dim={2}", frame.Length, bitlength, _serializer.Dimensions);
				return;
			}
			Send(_serializer.SerializeGray(frame, bitlength));
		}

		public void SendColoredGray2(byte[][] planes, Color[] palette)
		{
			if (planes.Length == 0) {
				return;
			}
			Send(_serializer.SerializeColoredGray2(planes, palette));
		}

		public void SendColoredGray4(byte[][] planes, Color[] palette)
		{
			if (planes.Length == 0) {
				return;
			}
			Send(_serializer.SerializeColoredGray4(planes, palette));
		}

		public void SendRgb24(byte[] frame) => Send(_serializer.SerializeRgb24(frame));

		public void SendGameName(string gameName) => Send(_serializer.SerializeGameName(gameName));

		public void SendDimensions(Dimensions dim) => Send(_serializer.SerializeDimensions(dim));

		public void SendColor(Color color) => Send(_serializer.SerializeColor(color));

		public void SendPalette(Color[] colors) => Send(_serializer.SerializePalette(colors));

		public void SendClearColor() => Send(_serializer.SerializeClearColor());

		public void SendClearPalette() => Send(_serializer.SerializeClearPalette());

		protected override void OnMessage(MessageEventArgs e)
		{
			if (e.Data == "init") {
				_dest.Init(this);
			}
			Logger.Info("Got message from client: {0}", e.Data);
		}

		protected override void OnError(ErrorEventArgs e)
		{
			Logger.Error(e.Exception, "Websock error: {0}", e.Message);
			_dest.Closed(this);
		}

		protected override void OnOpen()
		{
			Logger.Info("Websocket opened.");
		}

		protected override void OnClose(CloseEventArgs e)
		{
			_dest.Closed(this);
		}
	}

}
