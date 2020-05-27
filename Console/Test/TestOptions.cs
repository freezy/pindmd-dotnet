using CommandLine;
using DmdExt.Common;
using LibDmd;

namespace DmdExt.Test
{
	class TestOptions : BaseOptions
	{

		[Option("format", HelpText = "Try to output in that frame format. One of: [ rgb24, gray2, gray4, coloredgray2, coloredgray4 ].")]
		public FrameFormat FrameFormat { get; set; } = FrameFormat.Bitmap;

		[Option("size", HelpText = "Source frame dimensions. One of: [ 128x32, 192x64, 256x64 ]. Default: 128x32")]
		public string FrameSize { get; set; } = "128x32";

		[ParserState]
		public IParserState LastParserState { get; set; }
	}

}
