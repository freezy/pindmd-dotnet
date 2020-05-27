using System.Windows.Media;
using CommandLine;
using LibDmd.Common;
using LibDmd.DmdDevice;
using LibDmd.Input;
using LibDmd.Output.Virtual.AlphaNumeric;

namespace DmdExt.Common
{
	internal abstract class BaseOptions : IConfiguration
	{
		[Option('d', "destination", HelpText = "The destination where the DMD data is sent to. One of: [ auto, pindmdv1, pindmdv2, pindmdv3, pin2dmd, pin2dmdxl, virtual, alphanumeric, network ]. Default: \"virtual\".")]
		public DestinationType Destination { get; set; } = DestinationType.Virtual;

		[Option('r', "resize", HelpText = "How the source image is resized. One of: [ stretch, fill, fit ]. Default: \"stretch\".")]
		public ResizeMode Resize { get; set; } = ResizeMode.Stretch;

		[Option("no-virtual", HelpText = "Explicitly disables the virtual DMD when destination is \"auto\". Default: false.")]
		public bool NoVirtualDmd { get; set; } = false;

		[Option("virtual-stay-on-top", HelpText = "Makes the virtual DMD stay on top of other application windows. Default: false.")]
		public bool VirtualDmdOnTop { get; set; } = false;

		[OptionArray("virtual-position", HelpText = "Position and size of virtual DMD. Four values: <Left> <Top> <Width> [<Height>]. Height is optional and can be used for custom aspect ratio. Default: \"0 0 1024\".")]
		public int[] VirtualDmdPosition { get; set; } = { 0, 0, 1024 };

		[Option("virtual-dotsize", HelpText = "Scale the dot size of the virtual DMD. Default: 1")]
		public double VirtualDmdDotSize { get; set; } = 1;

		[Option('c', "color", HelpText = "Sets the color of a grayscale source that is rendered on an RGB destination. Default: ff3000")]
		public string RenderColor { get; set; } = "ff3000";

		[Option("flip-x", HelpText = "Flips the image horizontally (left/right). Default: false.")]
		public bool FlipHorizontally { get; set; } = false;

		[Option("flip-y", HelpText = "Flips the image vertically (top/down). Default: false.")]
		public bool FlipVertically { get; set; } = false;

		[Option('p', "port", HelpText = "Force COM port for PinDMDv3 devices. Example: \"COM3\".")]
		public string Port { get; set; } = null;

		[Option("output-delay", HelpText = "How long to wait in milliseconds for data to be sent to the device. Default: 25.")]
		public int OutputDelay { get; set; } = 25;

		[Option('q', "quit-when-done", HelpText = "Exit the program when finished, e.g. when Pinball FX2 doesn't receive any frames anymore. Default: false")]
		public bool QuitWhenDone { get; set; } = false;

		[Option("quit-after", HelpText = "Exit after n milliseconds. If set to -1, waits indefinitely or until source finishes when -q used. Default: -1")]
		public int QuitAfter { get; set; } = -1;

		[Option("no-clear", HelpText = "Don't clear screen when quitting. Default: false.")]
		public bool NoClear { get; set; } = false;

		[Option('o', "output-to-file", HelpText = "If set, writes all frames as PNG bitmaps to the provided folder.")]
		public string SaveToFile { get; set; }

		[Option("pinup", HelpText = "If set, enable output to PinUP. The value is the name of the game.")]
		public string PinUpName { get; set; } = null;

		[Option("use-ini", HelpText = "If set, use options from DmdDevice.ini.")]
		public string DmdDeviceIni { get; set; } = null;

		[Option("color-matrix", HelpText = "Color matrix to use for Pixelcade displays. Default: RBG.")]
		public ColorMatrix ColorMatrix { get; set; } = ColorMatrix.Rbg;

		[Option("url", HelpText = "Websocket URL for streaming via network. Default: ws://localhost/server")]
		public string WebsocketUrl { get; set; } = null;


		public IGlobalConfig Global { get; }
		public IVirtualDmdConfig VirtualDmd { get; }
		public IVirtualAlphaNumericDisplayConfig VirtualAlphaNumericDisplay { get; }
		public IPinDmd1Config PinDmd1 { get; }
		public IPinDmd2Config PinDmd2 { get; }
		public IPinDmd3Config PinDmd3 { get; }
		public IPin2DmdConfig Pin2Dmd { get; }
		public IPin2DmdConfig Pin2DmdXL { get; }
		public IPixelcadeConfig Pixelcade { get; }
		public IVideoConfig Video { get; }
		public IGifConfig Gif { get; }
		public IBitmapConfig Bitmap { get; }
		public IVpdbConfig VpdbStream { get; }
		public IBrowserConfig BrowserStream { get; }
		public INetworkConfig NetworkStream { get; }
		public IPinUpConfig PinUp { get; }

		protected BaseOptions()
		{
			Global = new GlobalConfig(this);
			VirtualDmd = new VirtualDmdOptions(this);
			VirtualAlphaNumericDisplay = new VirtualAlphaNumericDisplayOptions(this);
			PinDmd1 = new PinDmd1Options(this);
			PinDmd2 = new PinDmd2Options(this);
			PinDmd3 = new PinDmd3Options(this);
			Pin2Dmd = new Pin2DmdOptions(this);
			Pin2DmdXL = new Pin2DmdXLOptions(this);
			Pixelcade = new PixelcadeOptions(this);
			Video = new VideoOptions();
			Gif = new GifOptions();
			Bitmap = new BitmapOptions(this);
			VpdbStream = new VpdbOptions();
			BrowserStream = new BrowserOptions();
			NetworkStream = new NetworkOptions(this);
			PinUp = new PinUpOptions(this);
		}

		public enum DestinationType
		{
			Auto, PinDMDv1, PinDMDv2, PinDMDv3, PIN2DMD, PIN2DMDXL, PIXELCADE, Virtual, AlphaNumeric, Network
		}

		public void Validate()
		{
			if (!ColorUtil.IsColor(RenderColor)) {
				throw new InvalidOptionException("Argument --color must be a valid RGB color. Example: \"ff0000\".");
			}

			if (VirtualDmdPosition.Length != 3 && VirtualDmdPosition.Length != 4) {
				throw new InvalidOptionException("Argument --virtual-position must have three or four values: \"<Left> <Top> <Width> [<Height>]\".");
			}

			if (VirtualDmdDotSize <= 0 || VirtualDmdDotSize > 2) {
				throw new InvalidOptionException("Argument --virtual-dotsize must be larger than 0 and smaller than 10.");
			}
		}
	}

	internal class GlobalConfig : IGlobalConfig
	{
		private readonly BaseOptions _options;

		public GlobalConfig(BaseOptions options)
		{
			_options = options;
		}

		public ResizeMode Resize => _options.Resize;
		public bool FlipHorizontally => _options.FlipHorizontally;
		public bool FlipVertically => _options.FlipVertically;
		public bool Colorize => false;
		public bool QuitWhenDone => _options.QuitWhenDone;
		public int QuitAfter => _options.QuitAfter;
		public bool NoClear => _options.NoClear;
		public Color DmdColor => ColorUtil.ParseColor(_options.RenderColor);
	}

	internal class VirtualDmdOptions : IVirtualDmdConfig
	{
		private readonly BaseOptions _options;

		public VirtualDmdOptions(BaseOptions options)
		{
			_options = options;
		}

		public bool Enabled => _options.Destination == BaseOptions.DestinationType.Auto && !_options.NoVirtualDmd
							|| _options.Destination == BaseOptions.DestinationType.Virtual;
		public bool StayOnTop => _options.VirtualDmdOnTop;
		public bool IgnoreAr => _options.VirtualDmdPosition.Length == 4;
		public bool UseRegistryPosition => false;
		public double Left => _options.VirtualDmdPosition[0];
		public double Top => _options.VirtualDmdPosition[1];
		public double Width => _options.VirtualDmdPosition[2];

		public double Height => _options.VirtualDmdPosition.Length == 4
			? _options.VirtualDmdPosition[3]
			: (int) ((double)_options.VirtualDmdPosition[2] / 4);

		public double DotSize => _options.VirtualDmdDotSize;

		public bool HasGameOverride(string key)
		{
			return false;
		}
	}

	internal class VirtualAlphaNumericDisplayOptions : IVirtualAlphaNumericDisplayConfig
	{
		private readonly BaseOptions _options;

		public VirtualAlphaNumericDisplayOptions(BaseOptions options)
		{
			_options = options;
		}

		public bool StayOnTop => _options.VirtualDmdOnTop;
		public bool Enabled => _options.Destination == BaseOptions.DestinationType.AlphaNumeric;
		public RasterizeStyleDefinition Style { get; } = new RasterizeStyleDefinition();
	}

	internal class PinDmd1Options : IPinDmd1Config
	{
		private readonly BaseOptions _options;

		public PinDmd1Options(BaseOptions options)
		{
			_options = options;
		}

		public bool Enabled => _options.Destination == BaseOptions.DestinationType.Auto ||
		                       _options.Destination == BaseOptions.DestinationType.PinDMDv1;
	}

	internal class PinDmd2Options : IPinDmd2Config
	{
		private readonly BaseOptions _options;

		public PinDmd2Options(BaseOptions options)
		{
			_options = options;
		}

		public bool Enabled => _options.Destination == BaseOptions.DestinationType.Auto ||
		                       _options.Destination == BaseOptions.DestinationType.PinDMDv2;
	}

	internal class PinDmd3Options : IPinDmd3Config
	{
		private readonly BaseOptions _options;

		public PinDmd3Options(BaseOptions options)
		{
			_options = options;
		}

		public bool Enabled => _options.Destination == BaseOptions.DestinationType.Auto ||
		                       _options.Destination == BaseOptions.DestinationType.PinDMDv3;
		public string Port => _options.Port;
	}

	internal class Pin2DmdOptions : IPin2DmdConfig
	{
		protected readonly BaseOptions _options;

		public Pin2DmdOptions(BaseOptions options)
		{
			_options = options;
		}

		public virtual bool Enabled => _options.Destination == BaseOptions.DestinationType.Auto ||
		                       _options.Destination == BaseOptions.DestinationType.PIN2DMD;

		public int Delay => _options.OutputDelay;
	}

	internal class Pin2DmdXLOptions : Pin2DmdOptions
	{

		public Pin2DmdXLOptions(BaseOptions options) : base(options) {
		}

		public override bool Enabled => _options.Destination == BaseOptions.DestinationType.Auto ||
							   _options.Destination == BaseOptions.DestinationType.PIN2DMDXL;
	}


	internal class PixelcadeOptions : IPixelcadeConfig
	{
		private readonly BaseOptions _options;

		public PixelcadeOptions(BaseOptions options)
		{
			_options = options;
		}

		public bool Enabled => _options.Destination == BaseOptions.DestinationType.Auto ||
		                       _options.Destination == BaseOptions.DestinationType.PIXELCADE;
		public string Port => _options.Port;
		public ColorMatrix ColorMatrix => _options.ColorMatrix;
	}

	internal class VideoOptions : IVideoConfig
	{
		public bool Enabled => false;
		public string Path => null;
	}

	internal class GifOptions : IGifConfig
	{
		public bool Enabled => false;
		public string Path => null;
	}

	internal class BitmapOptions : IBitmapConfig
	{
		private readonly BaseOptions _options;

		public BitmapOptions(BaseOptions options)
		{
			_options = options;
		}

		public bool Enabled => _options.SaveToFile != null;
		public string Path => _options.SaveToFile;
	}

	internal class VpdbOptions : IVpdbConfig
	{
		public bool Enabled => false;
		public string EndPoint => null;
	}

	internal class BrowserOptions : IBrowserConfig
	{
		public bool Enabled => false;
		public int Port => 0;
	}

	internal class NetworkOptions : INetworkConfig
	{
		private readonly BaseOptions _options;

		public NetworkOptions(BaseOptions options)
		{
			_options = options;
		}

		public bool Enabled => _options.Destination == BaseOptions.DestinationType.Network;
		public string Url => _options.WebsocketUrl;
	}

	internal class PinUpOptions : IPinUpConfig
	{
		private readonly BaseOptions _options;

		public PinUpOptions(BaseOptions options)
		{
			_options = options;
		}

		public bool Enabled => _options.PinUpName != null;
		public string GameName => _options.PinUpName;
	}
}
