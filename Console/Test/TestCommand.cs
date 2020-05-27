using System;
using System.Reactive.Subjects;
using System.Windows.Media.Imaging;
using DmdExt.Common;
using LibDmd;
using LibDmd.DmdDevice;
using LibDmd.Frame;
using LibDmd.Input;
using LibDmd.Input.FileSystem;
using LibDmd.Input.PinMame;

namespace DmdExt.Test
{
	class TestCommand : BaseCommand
	{
		private readonly IConfiguration _config;
		private readonly TestOptions _testOptions;
		private RenderGraph _graph;

		private static Uri TestImage128x32 = new Uri("pack://application:,,,/dmdext;component/Test/TestImage.png");
		private static Uri TestImage192x64 = new Uri("pack://application:,,,/dmdext;component/Test/TestImage192x64.png");
		private static Uri TestImage256x64 = new Uri("pack://application:,,,/dmdext;component/Test/TestImage256x64.png");

		public TestCommand(IConfiguration config, TestOptions testOptions)
		{
			_config = config;
			_testOptions = testOptions;
		}

		protected override void CreateRenderGraphs(RenderGraphCollection graphs)
		{
			// define renderers
			var renderers = GetRenderers(_config);

			if (_config.VirtualAlphaNumericDisplay.Enabled) {
				var alphaNumericFrame = new AlphaNumericFrame(NumericalLayout.__2x20Alpha,
					new ushort[] {
						0, 10767, 2167, 8719, 0, 2109, 8713, 6259, 56, 2157, 0, 4957, 0, 8719, 62, 8719, 121, 2157, 0,
						0, 0, 0, 5120, 8704, 16640, 0, 0, 0, 0, 2112, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
						0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
					});
				_graph = new RenderGraph {
					Source = new VpmAlphaNumericSource(alphaNumericFrame),
					Destinations = renderers,
					Resize = _config.Global.Resize,
					FlipHorizontally = _config.Global.FlipHorizontally,
					FlipVertically = _config.Global.FlipVertically
				};

			} else {

				// figure out with image
				if (!Enum.TryParse($"Size{_testOptions.FrameSize}", true, out FrameSize frameSize)) {
					Logger.Warn("Ignoring invalid frame size \"{0}\".", _testOptions.FrameSize);
					frameSize = FrameSize.Size128x32;
				}

				Uri uri;
				switch (frameSize) {
					case FrameSize.Size192x64:
						uri = TestImage192x64;
						break;
					case FrameSize.Size256x64:
						uri = TestImage256x64;
						break;
					default:
						uri = TestImage128x32;
						break;
				}

				// retrieve image
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = uri;
				bmp.EndInit();
				var frame = new BmpFrame(bmp);

				ISource source;
				switch (_testOptions.FrameFormat) {
					case FrameFormat.Gray2:
						source = new ImageSourceGray2(frame);
						break;

					case FrameFormat.Gray4:
						source = new ImageSourceGray4(frame);
						break;

					case FrameFormat.ColoredGray2:
						source = new ImageSourceColoredGray2(frame);
						break;

					case FrameFormat.ColoredGray4:
						source = new ImageSourceColoredGray4(frame);
						break;

					default:
						source = new ImageSourceBitmap(frame);
						break;
				}

				// chain them up
				source.Dimensions = new BehaviorSubject<Dimensions>(frame.Dimensions);
				_graph = new RenderGraph {
					Source = source,
					Destinations = renderers,
					Resize = _config.Global.Resize,
					FlipHorizontally = _config.Global.FlipHorizontally,
					FlipVertically = _config.Global.FlipVertically
				};
			}

			graphs.Add(_graph);
		}
	}

	public enum FrameSize {
		Size128x32, Size192x64, Size256x64,
	}
}
