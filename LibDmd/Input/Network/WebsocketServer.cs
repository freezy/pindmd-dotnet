using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Windows.Media;
using LibDmd.Frame;
using LibDmd.Output;
using LibDmd.Output.Network;
using NLog;
using WebSocketSharp;
using WebSocketSharp.Server;
using HttpStatusCode = WebSocketSharp.Net.HttpStatusCode;
using Logger = NLog.Logger;

namespace LibDmd.Input.Network
{
	public class WebsocketServer : ISocketAction
	{
		internal WebsocketGray2Source Gray2Source = new WebsocketGray2Source();
		internal WebsocketGray4Source Gray4Source = new WebsocketGray4Source();
		internal WebsocketColoredGray2Source ColoredGray2Source = new WebsocketColoredGray2Source();
		internal WebsocketColoredGray4Source ColoredGray4Source = new WebsocketColoredGray4Source();
		internal WebsocketRgb24Source Rgb24Source = new WebsocketRgb24Source();

		private readonly HttpServer _server;
		private readonly List<DmdSocket> _sockets = new List<DmdSocket>();
		private RenderGraphCollection _graphs;
		private DmdFrame _dmdFrame = new DmdFrame();
		private ColoredFrame _coloredFrame = new ColoredFrame();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string Html = "<!DOCTYPE html><html><head><title>DmdExt Websocket Server</title></head><body><center style=\"margin-top=50px\"><h1>DmdExt Websocket Server</h1><p>Nothing to see here. Send frames to {ws_url} and you'll see them on your display.</p></center></body></html>";

		public WebsocketServer(string ip, int port, string path) {

			Logger.Info("Starting server at http://{0}:{1}{2}...", ip, port, path);
			_server = new HttpServer(IPAddress.Parse(ip), port);
			var html = Html.Replace("{ws_url}", ip + path);
			_server.OnGet += (sender, e) => {
				var res = e.Response;
				var data = Encoding.UTF8.GetBytes(html);
				res.StatusCode = (int)HttpStatusCode.OK;
				res.ContentType = "text/html";
				res.ContentEncoding = Encoding.UTF8;
				res.ContentLength64 = data.Length;
				res.OutputStream.Write(data, 0, data.Length);
			};
			_server.AddWebSocketService(path, () => {
				var socket = new DmdSocket(this);
				_sockets.Add(socket);
				return socket;
			});
			_server.Start();
			if (_server.IsListening) {
				Logger.Info("Server listening, connect to ws://{0}:{1}{2}...", ip, port, path);
			}
		}

		public void SetupGraphs(RenderGraphCollection graphs, List<IDestination> renderers)
		{
			_graphs = graphs;
			graphs.Add(new RenderGraph {
				Name = "2-bit Websocket Graph",
				Source = Gray2Source,
				Destinations = renderers,
			});
			graphs.Add(new RenderGraph {
				Name = "4-bit Websocket Graph",
				Source = Gray4Source,
				Destinations = renderers,
			});
			graphs.Add(new RenderGraph {
				Name = "Colored 2-bit Websocket Graph",
				Source = ColoredGray2Source,
				Destinations = renderers,
			});
			graphs.Add(new RenderGraph {
				Name = "Colored 4-bit Websocket Graph",
				Source = ColoredGray4Source,
				Destinations = renderers,
			});
			graphs.Add(new RenderGraph {
				Name = "24-bit RGB Websocket Graph",
				Source = Rgb24Source,
				Destinations = renderers,
			});
		}

		public void Dispose()
		{
			_server.Stop();
		}

		internal void Closed(DmdSocket socket)
		{
			_sockets.Remove(socket);
			Logger.Debug("WebSocket {0} closed", socket.ID);
		}

		public void OnColor(Color color) => _graphs.SetColor(color);

		public void OnPalette(Color[] palette) => _graphs.SetPalette(palette, -1);

		public void OnClearColor() => _graphs.ClearColor();

		public void OnClearPalette() => _graphs.ClearPalette();

		public void OnDimensions(Dimensions dimensions)
		{
			_dmdFrame.Update(dimensions);
			_coloredFrame.Update(dimensions);
			Gray2Source.Dimensions.OnNext(dimensions);
		}

		public void OnGameName(string gameName)
		{
			Logger.Info("OnGameName: {0}", gameName);
		}

		public void OnRgb24(uint timestamp, byte[] frame) => Rgb24Source.FramesRgb24.OnNext(_dmdFrame.Update(frame));

		public void OnColoredGray4(uint timestamp, Color[] palette, byte[][] planes)
			=> ColoredGray4Source.FramesColoredGray4.OnNext(_coloredFrame.Update(planes, palette));

		public void OnColoredGray2(uint timestamp, Color[] palette, byte[][] planes)
			=> ColoredGray2Source.FramesColoredGray2.OnNext(_coloredFrame.Update(planes, palette));

		public void OnGray4(uint timestamp, byte[] frame) => Gray4Source.FramesGray4.OnNext(_dmdFrame.Update(frame));

		public void OnGray2(uint timestamp, byte[] frame) => Gray2Source.FramesGray2.OnNext(_dmdFrame.Update(frame));
	}

	public class DmdSocket : WebSocketBehavior
	{
		private readonly WebsocketServer _src;
		private readonly WebsocketSerializer _serializer = new WebsocketSerializer();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public DmdSocket(WebsocketServer src) {
			_src = src;
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			_serializer.Unserialize(e.RawData, _src);
		}

		protected override void OnClose(CloseEventArgs e)
		{
			_src.Closed(this);
		}

		protected override void OnError(ErrorEventArgs e)
		{
			Logger.Error(e.Exception, "Websock error: {0}", e.Message);
			_src.Closed(this);
		}

		protected override void OnOpen()
		{
			Logger.Debug("New WebSocket client {0}", ID);
		}
	}
}
