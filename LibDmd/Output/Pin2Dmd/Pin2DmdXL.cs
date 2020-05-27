using System.Threading;
using LibDmd.Common;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Output.Pin2Dmd
{
	public class Pin2DmdXL : Pin2DmdBase, IGray2Destination, IGray4Destination,
		IColoredGray2Destination, IColoredGray4Destination, IMultiSizeDestination
	{
		public override string Name { get; } = "PIN2DMD XL";

		public Dimensions[] Sizes { get; } = {Dim128x32, Dim192x64};

		private readonly byte[] _frameBufferGray4XL;

		private static Pin2DmdXL _instance;
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public Pin2DmdXL()
		{
			var size = (Dim192x64.Surface * 4 / 8) + 4;
			_frameBufferGray4XL = new byte[size];
			_frameBufferGray4XL[0] = 0x81; // frame sync bytes
			_frameBufferGray4XL[1] = 0xC3;
			_frameBufferGray4XL[2] = 0xE8;
			_frameBufferGray4XL[3] = 0x00;
		}

		/// <summary>
		/// Returns the current instance of the PIN2DMD API. In any case,
		/// the instance get (re-)initialized.
		/// </summary>
		/// <returns></returns>
		public static Pin2DmdXL GetInstance(int outputDelay)
		{
			if (_instance == null) {
				_instance = new Pin2DmdXL { Delay = outputDelay };
			}
			_instance.Init();
			return _instance;
		}

		protected override bool HasValidName(string name)
		{
			return name.Contains("PIN2DMD XL");
		}

		public override void RenderGray2(DmdFrame frame)
		{
			Logger.Debug("[PIN2DMD-XL] Gray2 at {0} ({1} bytes)", frame.Dimensions, frame.Data.Length);
			if (frame.Dimensions == Dim128x32) {
				base.RenderGray2(frame);

			} else {
				// 2-bit frames are rendered as 4-bit
				RenderGray4XL(frame.ConvertGrayToGray(0x0, 0x1, 0x4, 0xf), 0x06);
			}
		}

		public override void RenderGray4(DmdFrame frame)
		{
			Logger.Debug("[PIN2DMD-XL] Gray4 at {0} ({1} bytes)", frame.Dimensions, frame.Data.Length);
			if (frame.Dimensions == Dim128x32) {
				base.RenderGray4(frame);

			} else {
				RenderGray4XL(frame, 0x0c);
			}
		}

		public override void RenderColoredGray2(ColoredFrame frame)
		{
			Logger.Debug("[PIN2DMD-XL] Colored gray2 at {0}", frame.Dimensions);
			if (frame.Dimensions == Dim128x32) {
				base.RenderColoredGray2(frame);

			} else {
				SetPalette(frame.Palette, frame.PaletteIndex);
				var frameGray = frame.ConvertToGray(0x0, 0x1, 0x4, 0xf);
				RenderGray4XL(frameGray, 0x06);
			}
		}

		public override void RenderColoredGray4(ColoredFrame frame)
		{
			Logger.Debug("[PIN2DMD-XL] Colored gray4 at {0}", frame.Dimensions);
			if (frame.Dimensions == Dim128x32) {
				base.RenderColoredGray4(frame);

			} else {
				SetPalette(frame.Palette, frame.PaletteIndex);
				RenderGray4XL(frame.Planes, 0x0c);
			}
		}

		public void ClearDisplay()
		{
			var buffer = new byte[6148];
			buffer[0] = 0x81;
			buffer[1] = 0xC3;
			buffer[2] = 0xE8;
			buffer[3] = 0x0C;
			RenderRaw(buffer);
			Thread.Sleep(Delay);
		}

		private void RenderGray4XL(DmdFrame frame, byte byte3)
		{
			var planes = FrameUtil.Split(frame.Dimensions, 4, frame.Data);
			RenderGray4XL(planes, byte3);
		}

		private void RenderGray4XL(byte[][] planes, byte byte3)
		{
			// copy to buffer
			var changed = FrameUtil.Copy(planes, _frameBufferGray4XL, 4);

			changed = changed && _frameBufferGray4XL[3] == byte3;
			_frameBufferGray4XL[3] = byte3;

			// send frame buffer to device
			if (changed) {
				Logger.Debug("[PIN2DMD-XL] Sending {0} bytes of gray4 ({1}).", _frameBufferGray4XL.Length, Dim192x64);
				RenderRaw(_frameBufferGray4XL);
			} else {
				Logger.Debug("[PIN2DMD-XL] Skipping identical gray4 frame.");
			}
		}
	}
}
