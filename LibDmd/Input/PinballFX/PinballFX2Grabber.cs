namespace LibDmd.Input.PinballFX
{
	public class PinballFX2Grabber : PinballFXGrabber
	{
		public override string Name { get; } = "Pinball FX2";

		protected override string GetProcessName()
		{
			return "Pinball FX2";
		}
	}
}
