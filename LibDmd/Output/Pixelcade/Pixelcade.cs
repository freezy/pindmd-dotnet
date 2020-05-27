using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Media;
using LibDmd.Common;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Output.Pixelcade
{
	/// <summary>
	/// Output target for Pixelcade devices.
	/// </summary>
	/// <see cref="http://pixelcade.org/"/>
	public class Pixelcade : IRgb24Destination, IFixedSizeDestination
	{
		public string Name { get; } = "Pixelcade";
		public bool IsAvailable { get; private set; }
		private int Delay { get; } = 100;

		public Dimensions FixedSize { get; } = new Dimensions(128, 32);

		private const int ReadTimeoutMs = 100;
		private const byte RgbLedMatrixFrameCommandByte = 0x1F;
		private const byte RgbLedMatrixEnableCommandByte = 0x1E;
		private const byte ResponseEstablishConnection = 0x00;

		/// <summary>
		/// Firmware string read from the device if connected
		/// </summary>
		public string Firmware { get; private set; }

		/// <summary>
		/// Manually overriden port name. If set, don't loop through available
		/// ports in order to find the device.
		/// </summary>
		public string Port { get; set; }

		/// <summary>
		/// Color matrix to use, default it RGB.
		/// </summary>
		public ColorMatrix ColorMatrix { get; set; } = ColorMatrix.Rgb;

		private static Pixelcade _instance;
		private readonly byte[] _frameBuffer;
		private bool _lastFrameFailed;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// If device is initialized in raw mode, this is the raw device.
		/// </summary>
		private SerialPort _serialPort;

		/// <summary>
		/// Returns the current instance of the Pixelcade API.
		/// </summary>
		/// <returns>New or current instance</returns>
		public static Pixelcade GetInstance()
		{
			if (_instance == null) {
				_instance = new Pixelcade();
			}
			_instance.Init();
			return _instance;
		}

		/// <summary>
		/// Returns the current instance of the Pixelcade API.
		/// </summary>
		/// <param name="port">Don't loop through available ports but use this COM port name.</param>
		/// <param name="colorMatrix">RGB or RBG</param>
		/// <returns>New or current instance</returns>
		public static Pixelcade GetInstance(string port, ColorMatrix colorMatrix)
		{
			if (_instance == null) {
				_instance = new Pixelcade { Port = port, ColorMatrix = colorMatrix };
			}
			_instance.Init();
			return _instance;
		}

		/// <summary>
		/// Constructor, initializes the DMD.
		/// </summary>
		private Pixelcade()
		{
			_frameBuffer = new byte[FixedSize.Surface * 3 / 2 + 1];
			_frameBuffer[0] = RgbLedMatrixFrameCommandByte;
		}

		public void Init()
		{
			if (Port != null && Port.Trim().Length > 0) {
				IsAvailable = Connect(Port);

			} else {
				var ports = SerialPort.GetPortNames();
				foreach (var portName in ports) {
					IsAvailable = Connect(portName);
					if (IsAvailable) {
						break;
					}
				}
			}

			if (!IsAvailable) {
				Logger.Info("Pixelcade device not found.");
				return;
			}

			// put the matrix into stream mode
			EnableRgbLedMatrix(4, 16);
		}
		private bool Connect(string port)
		{
			try {
				Logger.Info("Checking port {0} for Pixelcade...", port);

				// since pixelcade is USB, we don't need to set the baud rate, parity bit, etc
				_serialPort = new SerialPort(port);
				_serialPort.ReceivedBytesThreshold = 1;
				_serialPort.DtrEnable = true;
				_serialPort.ReadTimeout = ReadTimeoutMs;
				System.Threading.Thread.Sleep(Delay);
				_serialPort.Open();

				// let's assume opening a connection results in what IncomingState.handleEstablishConnection() receives...
				var result = new byte[1 + 4 + 8 + 8 + 8];
				_serialPort.Read(result, 0, 1 + 4 + 8 + 8 + 8);

				if (result[0] != ResponseEstablishConnection) {
					throw new Exception($"Expected new connection to return 0x0, but got {result[0]}");
				}

				var magic = Encoding.UTF8.GetString(result.Skip(1).Take(4).ToArray());

				if (magic != "IOIO") {
					throw new Exception($"Expected magic code to equal IOIO but got {magic}");
				}

				var hardwareId = Encoding.UTF8.GetString(result.Skip(1+4).Take(8).ToArray());
				var bootloaderId = Encoding.UTF8.GetString(result.Skip(1+4+8).Take(8).ToArray());
				Firmware = Encoding.UTF8.GetString(result.Skip(1+4+8+8).Take(8).ToArray());
				Logger.Info("Found Pixelcade device on {0}.", port);
				Logger.Debug(" Hardware ID: {0}", hardwareId);
				Logger.Debug(" Bootloader ID: {0}", bootloaderId);
				Logger.Debug(" Firmware: {0}", Firmware);
				return true;

			} catch (Exception e) {
				Logger.Error("Error: {0}", e.Message.Trim());
				if (_serialPort != null && _serialPort.IsOpen) {
					_serialPort.Close();
					System.Threading.Thread.Sleep(Delay); // otherwise the next device will fail
				}
			}
			return false;
		}

		public void RenderRgb24(DmdFrame frame)
		{
			// convert rgb24 to rgb565
			var frame565 = ImageUtil.ConvertToRgb565(FixedSize, frame.Data);

			// split into planes to send over the wire
			var newFrame = new byte[FixedSize.Surface * 3 / 2];
			FrameUtil.SplitIntoRgbPlanes(frame565, FixedSize.Width, 16, newFrame, ColorMatrix);

			// copy to frame buffer
			var changed = FrameUtil.Copy(newFrame, _frameBuffer, 1);

			// send to device if changed
			if (changed) {
				RenderRaw(_frameBuffer);
			}
		}

		public void RenderRaw(byte[] data)
		{
			try {
				_serialPort.Write(data, 0, data.Length);
				_lastFrameFailed = false;

			} catch (Exception e) {
				if (!_lastFrameFailed) {
					Logger.Error("Error writing to serial port: {0}", e.Message);
					_lastFrameFailed = true;
				}
			}
		}

		public void ClearDisplay()
		{
			for (var i = 1; i < _frameBuffer.Length - 1; i++) {
				_frameBuffer[i] = 0;
			}
			if (_serialPort.IsOpen) {
				_serialPort.Write(_frameBuffer, 0, _frameBuffer.Length);
			}
		}

		public void SetColor(Color color)
		{
			// no palette support here.
		}

		public void SetPalette(Color[] colors, int index = -1)
		{
			// no palette support here.
		}

		public void ClearPalette()
		{
			ClearColor();
		}

		public void ClearColor()
		{
			SetColor(RenderGraph.DefaultColor);
		}

		public void Dispose()
		{
			if (_serialPort.IsOpen) {
				_serialPort.Close();
			}
		}

		private void EnableRgbLedMatrix(int shifterLen32, int rows)
		{
			_serialPort.Write(new[] {
				RgbLedMatrixEnableCommandByte,
				(byte)(shifterLen32 & 0x0F | ((rows == 8 ? 0 : 1) << 4))
			}, 0, 2);
		}
	}
}
