using System.Windows.Media;
using LibDmd.Input;
using LibDmd.Output.Virtual.AlphaNumeric;

namespace LibDmd.DmdDevice
{
	public interface IConfiguration
	{
		IGlobalConfig Global { get; }
		IVirtualDmdConfig VirtualDmd { get; }
		IVirtualAlphaNumericDisplayConfig VirtualAlphaNumericDisplay { get; }
		IPinDmd1Config PinDmd1 { get; }
		IPinDmd2Config PinDmd2 { get; }
		IPinDmd3Config PinDmd3 { get; }
		IPin2DmdConfig Pin2Dmd { get; }
		IPin2DmdConfig Pin2DmdXL { get; }
		IPixelcadeConfig Pixelcade { get; }
		IVideoConfig Video { get; }
		IGifConfig Gif { get; }
		IBitmapConfig Bitmap { get; }
		IVpdbConfig VpdbStream { get; }
		IBrowserConfig BrowserStream { get; }
		INetworkConfig NetworkStream { get; }
		IPinUpConfig PinUp { get; }
		void Validate();
	}

	public interface IGlobalConfig 
	{
		ResizeMode Resize { get; }
		bool FlipHorizontally { get; }
		bool FlipVertically { get; }
		bool Colorize { get; }
		bool QuitWhenDone { get; }
		int QuitAfter { get; }
		bool NoClear { get; }
		Color DmdColor { get; }
	}

	public interface IPinDmd1Config
	{
		bool Enabled { get; }
	}

	public interface IPinDmd2Config
	{
		bool Enabled { get; }
	}

	public interface IPinDmd3Config
	{
		bool Enabled { get; }
		string Port { get; }
	}

	public interface IPin2DmdConfig
	{
		bool Enabled { get; }
		int Delay { get; }
	}

	public interface IPixelcadeConfig
	{
		bool Enabled { get; }
		string Port { get; }
		ColorMatrix ColorMatrix { get; }
	}

	public interface IVirtualDmdConfig
	{
		bool Enabled { get; }
		bool StayOnTop { get; }
		bool IgnoreAr { get; }
		bool UseRegistryPosition { get; }
		double Left { get; }
		double Top { get; }
		double Width { get; }
		double Height { get; }
		double DotSize { get; }
		bool HasGameOverride(string key);
	}

	public interface IVirtualAlphaNumericDisplayConfig
	{
		bool Enabled { get; }
		bool StayOnTop { get; }
		RasterizeStyleDefinition Style { get; }
	}

	public interface IVideoConfig
	{
		bool Enabled { get; }
		string Path { get; }
	}

	public interface IGifConfig
	{
		bool Enabled { get; }
		string Path { get; }
	}

	public interface IBitmapConfig
	{
		bool Enabled { get; }
		string Path { get; }
	}

	public interface IBrowserConfig
	{
		bool Enabled { get; }
		int Port { get; }
	}

	public interface INetworkConfig
	{
		bool Enabled { get; }
		string Url { get; }
	}

	public interface IVpdbConfig
	{
		bool Enabled { get; }
		string EndPoint { get; }
	}

	public interface IPinUpConfig
	{
		bool Enabled { get; }
		string GameName { get; }
	}
}
