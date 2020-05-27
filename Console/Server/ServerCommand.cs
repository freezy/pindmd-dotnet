using DmdExt.Common;
using LibDmd;
using LibDmd.DmdDevice;
using LibDmd.Input.Network;

namespace DmdExt.Server
{
	class ServerCommand : BaseCommand
	{
		private readonly IConfiguration _config;
		private readonly ServerOptions _serverOptions;

		public ServerCommand(IConfiguration config, ServerOptions serverOptions) {
			_config = config;
			_serverOptions = serverOptions;
		}

		protected override void CreateRenderGraphs(RenderGraphCollection graphs)
		{
			var renderers = GetRenderers(_config);
			var websocketServer = new WebsocketServer(_serverOptions.Ip, _serverOptions.Port, _serverOptions.Path);
			websocketServer.SetupGraphs(graphs, renderers);
		}
	}
}
