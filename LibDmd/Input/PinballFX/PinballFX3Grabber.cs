namespace LibDmd.Input.PinballFX
{
	public class PinballFX3Grabber : PinballFXGrabber
	{
		public override string Name { get; } = "Pinball FX3";

		protected override string GetProcessName()
		{
			return "Pinball FX3";
		}
	}
}
