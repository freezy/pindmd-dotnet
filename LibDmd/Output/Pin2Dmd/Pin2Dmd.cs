using System;
using System.Threading;
using LibDmd.Frame;

namespace LibDmd.Output.Pin2Dmd
{
	public class Pin2Dmd : Pin2DmdBase, IGray2Destination, IGray4Destination,
		IColoredGray2Destination, IColoredGray4Destination,
		IRawOutput, IFixedSizeDestination
	{
		public override string Name { get; } = "PIN2DMD";

		public Dimensions FixedSize { get; } = new Dimensions(128, 32);

		private static Pin2Dmd _instance;

		/// <summary>
		/// Returns the current instance of the PIN2DMD API. In any case,
		/// the instance get (re-)initialized.
		/// </summary>
		/// <returns></returns>
		public static Pin2Dmd GetInstance(int outputDelay)
		{
			if (_instance == null) {
				_instance = new Pin2Dmd { Delay = outputDelay };
			}
			_instance.Init();
			return _instance;
		}

		protected override bool HasValidName(string name)
		{
			return name.Contains("PIN2DMD") && !name.Contains("PIN2DMD XL");
		}

		public void ClearDisplay()
		{
			var buffer = new byte[2052];
			buffer[0] = 0x81;
			buffer[1] = 0xC3;
			buffer[2] = 0xE7;
			buffer[3] = 0x00;
			RenderRaw(buffer);
			Thread.Sleep(Delay);
		}
	}
}
