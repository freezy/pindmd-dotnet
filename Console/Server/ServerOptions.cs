using CommandLine;
using DmdExt.Common;

namespace DmdExt.Server
{
	class ServerOptions : BaseOptions
	{
		[Option("ip", HelpText = "IP address to listen to. Put 0.0.0.0 to listen to all interfaces. Default: 127.0.0.1")]
		public string Ip { get; set; } = "127.0.0.1";

		[Option("port", HelpText = "WebSocket host to listen to. Default: 80")]
		public new int Port { get; set; } = 80;

		[Option("path", HelpText = "WebSocket path. Default: /dmd")]
		public string Path { get; set; } = "/dmd";

		[ParserState]
		public IParserState LastParserState { get; set; }
	}
}
