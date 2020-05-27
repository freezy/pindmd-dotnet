using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using LibDmd.Common;
using LibDmd.DmdDevice;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Output.Virtual.AlphaNumeric
{
	public class VirtualAlphanumericDestination : IAlphaNumericDestination
	{
		public string Name => "Virtual Alphanumeric Renderer";
		public bool IsAvailable => true;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly AlphaNumericResources Res = AlphaNumericResources.GetInstance();
		private static VirtualAlphanumericDestination _instance;

		private readonly Configuration _config;
		private readonly Dispatcher _dispatcher;
		private readonly RasterizeStyleDefinition _styleDef;

		private readonly Dictionary<int, VirtualAlphaNumericDisplay> _displays = new Dictionary<int, VirtualAlphaNumericDisplay>();
		private readonly Dictionary<int, ushort[]> _droppedData = new Dictionary<int, ushort[]>();
		private NumericalLayout _currentLayout = NumericalLayout.None;

		private bool _settingsOpen;
		private VirtualAlphaNumericSettings _settingWindow;
		private IDisposable _settingSubscription;

		private VirtualAlphanumericDestination(Dispatcher dispatcher, RasterizeStyleDefinition styleDef, Configuration config)
		{
			_dispatcher = dispatcher;
			_styleDef = styleDef;
			_config = config;
		}

		public static VirtualAlphanumericDestination GetInstance(Dispatcher dispatcher, RasterizeStyleDefinition styleDef, Configuration config)
		{
			return _instance ?? (_instance = new VirtualAlphanumericDestination(dispatcher, styleDef, config));
		}

		public void RenderAlphaNumeric(AlphaNumericFrame frame)
		{
			if (_currentLayout == NumericalLayout.None || _currentLayout != frame.SegmentLayout) {
				ShowDisplays(frame.SegmentLayout);
				_currentLayout = frame.SegmentLayout;
			}
			//Logger.Info("New frame type {0}", frame.SegmentLayout);

			switch (frame.SegmentLayout) {

				case NumericalLayout.__2x16Alpha:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 16).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 16, 16).ToArray());
					break;

				case NumericalLayout.__2x20Alpha:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 20).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 20, 20).ToArray());
					break;

				case NumericalLayout.__2x7Alpha_2x7Num:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 7).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 7, 7).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 14, 7).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 21, 7).ToArray());
					break;

				case NumericalLayout.__2x7Alpha_2x7Num_4x1Num:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 7).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 7, 7).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 14, 7).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 21, 7).ToArray());
					SendToDisplay(4, new ArraySegment<ushort>(frame.SegmentData, 28, 2).ToArray());
					SendToDisplay(5, new ArraySegment<ushort>(frame.SegmentData, 30, 2).ToArray());
					break;

				case NumericalLayout.__2x7Num_2x7Num_4x1Num:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 7).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 7, 7).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 14, 7).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 21, 7).ToArray());
					SendToDisplay(4, new ArraySegment<ushort>(frame.SegmentData, 28, 2).ToArray());
					SendToDisplay(5, new ArraySegment<ushort>(frame.SegmentData, 30, 2).ToArray());
					break;

				case NumericalLayout.__2x7Num_2x7Num_10x1Num:
					var data10x1 = new ushort[10];
					new ArraySegment<ushort>(frame.SegmentData, 28, 4).ToArray().CopyTo(data10x1, 0);
					new ArraySegment<ushort>(frame.SegmentDataExtended, 0, 6).ToArray().CopyTo(data10x1, 4);
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 7).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 7, 7).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 14, 7).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 21, 7).ToArray());
					SendToDisplay(5, data10x1);
					break;

				case NumericalLayout.__2x7Num_2x7Num_4x1Num_gen7:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 7).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 7, 7).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 14, 7).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 21, 7).ToArray());
					SendToDisplay(4, new ArraySegment<ushort>(frame.SegmentData, 28, 2).ToArray());
					SendToDisplay(5, new ArraySegment<ushort>(frame.SegmentData, 30, 2).ToArray());
					break;

				case NumericalLayout.__2x7Num10_2x7Num10_4x1Num:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 7).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 7, 7).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 14, 7).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 21, 7).ToArray());
					SendToDisplay(4, new ArraySegment<ushort>(frame.SegmentData, 28, 2).ToArray());
					SendToDisplay(5, new ArraySegment<ushort>(frame.SegmentData, 30, 2).ToArray());
					break;

				case NumericalLayout.__2x6Num_2x6Num_4x1Num:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 6).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 6, 6).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 12, 6).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 18, 6).ToArray());
					SendToDisplay(4, new ArraySegment<ushort>(frame.SegmentData, 24, 2).ToArray());
					SendToDisplay(5, new ArraySegment<ushort>(frame.SegmentData, 26, 2).ToArray());
					break;

				case NumericalLayout.__2x6Num10_2x6Num10_4x1Num:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 6).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 6, 6).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 12, 6).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 18, 6).ToArray());
					SendToDisplay(4, new ArraySegment<ushort>(frame.SegmentData, 24, 2).ToArray());
					SendToDisplay(5, new ArraySegment<ushort>(frame.SegmentData, 26, 2).ToArray());
					break;

				case NumericalLayout.__4x7Num10:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 7).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 7, 7).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 14, 7).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 21, 7).ToArray());
					break;

				case NumericalLayout.__6x4Num_4x1Num:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 4).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 4, 4).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 8, 4).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 12, 4).ToArray());
					SendToDisplay(4, new ArraySegment<ushort>(frame.SegmentData, 16, 4).ToArray());
					SendToDisplay(5, new ArraySegment<ushort>(frame.SegmentData, 20, 4).ToArray());
					SendToDisplay(6, new ArraySegment<ushort>(frame.SegmentData, 24, 2).ToArray());
					SendToDisplay(7, new ArraySegment<ushort>(frame.SegmentData, 26, 2).ToArray());
					break;

				case NumericalLayout.__2x7Num_4x1Num_1x16Alpha:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 7).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 7, 7).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 14, 4).ToArray());
					SendToDisplay(3, new ArraySegment<ushort>(frame.SegmentData, 18, 16).ToArray());
					break;

				case NumericalLayout.__1x16Alpha_1x16Num_1x7Num:
					SendToDisplay(0, new ArraySegment<ushort>(frame.SegmentData, 0, 16).ToArray());
					SendToDisplay(1, new ArraySegment<ushort>(frame.SegmentData, 16, 16).ToArray());
					SendToDisplay(2, new ArraySegment<ushort>(frame.SegmentData, 32, 7).ToArray());
					break;
			}
		}

		private void SendToDisplay(int displayNumber, ushort[] data)
		{
			if (_displays.ContainsKey(displayNumber)) {
				Logger.Debug("Sending data to display {0}...", displayNumber);
				_displays[displayNumber].AlphaNumericDisplay.RenderSegments(data);

			} else {
				Logger.Debug("Got data for display {0}, but it's now created yet, so saving for later.", displayNumber);
				_droppedData[displayNumber] = data;
			}
		}

		private void ShowDisplays(NumericalLayout layout)
		{
			//Logger.Info("New type of {0}", layout);
			// todo teardown current if open
			switch (layout) {

				case NumericalLayout.__2x16Alpha: // jokrz_l6 - Jokerz (L-6)
					ShowDisplay(0, 16, 1, SegmentType.Alphanumeric);
					ShowDisplay(1, 16, 1, SegmentType.Alphanumeric);
					break;

				case NumericalLayout.__2x20Alpha: // badgirls - Bad Girls
					ShowDisplay(0, 20, 1, SegmentType.Alphanumeric);
					ShowDisplay(1, 20, 1, SegmentType.Alphanumeric);
					break;

				case NumericalLayout.__2x7Alpha_2x7Num: // untested
					ShowDisplay(0, 7, 1, SegmentType.Alphanumeric);
					ShowDisplay(1, 7, 1, SegmentType.Alphanumeric);
					ShowDisplay(2, 7, 1, SegmentType.Numeric8);
					ShowDisplay(3, 7, 1, SegmentType.Numeric8);
					break;

				case NumericalLayout.__2x7Alpha_2x7Num_4x1Num: // hs_l3 - High Speed (L-3)
					ShowDisplay(0, 7, 1, SegmentType.Alphanumeric);
					ShowDisplay(1, 7, 1, SegmentType.Alphanumeric);
					ShowDisplay(2, 7, 1, SegmentType.Numeric8);
					ShowDisplay(3, 7, 1, SegmentType.Numeric8);
					ShowDisplay(4, 2, 1, SegmentType.Numeric8);
					ShowDisplay(5, 2, 1, SegmentType.Numeric8);
					break;

				case NumericalLayout.__2x7Num_2x7Num_4x1Num: //  sstb - Supersonic (7-digit conversion)
					ShowDisplay(0, 7, 1, SegmentType.Numeric8);
					ShowDisplay(1, 7, 1, SegmentType.Numeric8);
					ShowDisplay(2, 7, 1, SegmentType.Numeric8);
					ShowDisplay(3, 7, 1, SegmentType.Numeric8);
					ShowDisplay(4, 2, 1, SegmentType.Numeric8);
					ShowDisplay(5, 2, 1, SegmentType.Numeric8);
					break;

				case NumericalLayout.__2x7Num_2x7Num_10x1Num: // untested
					ShowDisplay(0, 7, 1, SegmentType.Numeric8);
					ShowDisplay(1, 7, 1, SegmentType.Numeric8);
					ShowDisplay(2, 7, 1, SegmentType.Numeric8);
					ShowDisplay(3, 7, 1, SegmentType.Numeric8);
					ShowDisplay(4, 10, 1, SegmentType.Numeric8);
					break;

				case NumericalLayout.__2x7Num_2x7Num_4x1Num_gen7: // bk_l4 - Black Knight (L-4)
					ShowDisplay(0, 7, 1, SegmentType.Numeric8);
					ShowDisplay(1, 7, 1, SegmentType.Numeric8);
					ShowDisplay(2, 7, 1, SegmentType.Numeric8);
					ShowDisplay(3, 7, 1, SegmentType.Numeric8);
					ShowDisplay(4, 2, 1, SegmentType.Numeric8);
					ShowDisplay(5, 2, 1, SegmentType.Numeric8);
					break;

				case NumericalLayout.__2x7Num10_2x7Num10_4x1Num: // sshtl_l3 - Space Shuttle (L-3)
					ShowDisplay(0, 7, 1, SegmentType.Numeric10);
					ShowDisplay(1, 7, 1, SegmentType.Numeric10);
					ShowDisplay(2, 7, 1, SegmentType.Numeric10);
					ShowDisplay(3, 7, 1, SegmentType.Numeric10);
					ShowDisplay(4, 2, 1, SegmentType.Numeric8);
					ShowDisplay(5, 2, 1, SegmentType.Numeric8);
					break;

				case NumericalLayout.__2x6Num_2x6Num_4x1Num: // topaz_l1 - Topaz (Shuffle) (L-1)
					ShowDisplay(0, 6, 1, SegmentType.Numeric8);
					ShowDisplay(1, 6, 1, SegmentType.Numeric8);
					ShowDisplay(2, 6, 1, SegmentType.Numeric8);
					ShowDisplay(3, 6, 1, SegmentType.Numeric8);
					ShowDisplay(4, 2, 1, SegmentType.Numeric8);
					ShowDisplay(5, 2, 1, SegmentType.Numeric8);
					break;

				case NumericalLayout.__2x6Num10_2x6Num10_4x1Num: // untested
					ShowDisplay(0, 6, 1, SegmentType.Numeric10);
					ShowDisplay(1, 6, 1, SegmentType.Numeric10);
					ShowDisplay(2, 6, 1, SegmentType.Numeric10);
					ShowDisplay(3, 6, 1, SegmentType.Numeric10);
					ShowDisplay(4, 2, 1, SegmentType.Numeric8);
					ShowDisplay(5, 2, 1, SegmentType.Numeric8);
					break;

				case NumericalLayout.__4x7Num10: // atlantis
					ShowDisplay(0, 7, 1, SegmentType.Numeric10);
					ShowDisplay(1, 7, 1, SegmentType.Numeric10);
					ShowDisplay(2, 7, 1, SegmentType.Numeric10);
					ShowDisplay(3, 7, 1, SegmentType.Numeric10);
					break;

				case NumericalLayout.__6x4Num_4x1Num: // alcat - Alley Cats
					ShowDisplay(0, 4, 1, SegmentType.Numeric8);
					ShowDisplay(1, 4, 1, SegmentType.Numeric8);
					ShowDisplay(2, 4, 1, SegmentType.Numeric8);
					ShowDisplay(3, 4, 1, SegmentType.Numeric8);
					ShowDisplay(4, 4, 1, SegmentType.Numeric8);
					ShowDisplay(5, 4, 1, SegmentType.Numeric8);
					ShowDisplay(6, 2, 1, SegmentType.Numeric8);
					ShowDisplay(7, 2, 1, SegmentType.Numeric8);
					break;

				case NumericalLayout.__2x7Num_4x1Num_1x16Alpha: // untested
					ShowDisplay(0, 7, 1, SegmentType.Numeric8);
					ShowDisplay(1, 7, 1, SegmentType.Numeric8);
					ShowDisplay(2, 4, 1, SegmentType.Numeric8);
					ShowDisplay(3, 16, 1, SegmentType.Alphanumeric);
					break;

				case NumericalLayout.__1x16Alpha_1x16Num_1x7Num:  // untested
					ShowDisplay(0, 16, 1, SegmentType.Alphanumeric);
					ShowDisplay(1, 16, 1, SegmentType.Numeric8);
					ShowDisplay(2, 7, 1, SegmentType.Numeric8);
					break;
			}
		}

		public void ClearDisplay()
		{
			Logger.Info("Clearing Display");
		}

		public void Dispose()
		{
			try {
				_settingSubscription?.Dispose();
				foreach (var display in _displays.Values) {
					display.Dispatcher.Invoke(() => display.Close());
				}
				Res.Clear();
				_displays.Clear();
				_droppedData.Clear();
				_instance = null;

			} catch (TaskCanceledException e) {
				Logger.Warn(e, "Could not hide DMD because task was already canceled.");
			}
		}

		private void ShowDisplay(int displayNumber, int numChars, int numLines, SegmentType type)
		{
			_dispatcher.Invoke(delegate {
				var display = CreateDisplay(displayNumber, numChars, numLines, type);
				var thread = new Thread(() => {
					SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(_dispatcher));
					display.Dispatcher.Invoke(() => {
						display.AlphaNumericDisplay.Init();
						display.Show();

						if (_droppedData.ContainsKey(displayNumber)) {
							Logger.Debug("Sending dropped frame to freshly created display {0}...", displayNumber);
							display.AlphaNumericDisplay.RenderSegments(_droppedData[displayNumber]);
							_droppedData.Remove(displayNumber);
						}
					});
					Dispatcher.Run();
				});
				display.Closed += (s, e) => Dispatcher.FromThread(thread).BeginInvokeShutdown(DispatcherPriority.Background);
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
			});
		}

		private VirtualAlphaNumericDisplay CreateDisplay(int displayNumber, int numChars, int numLines, SegmentType type)
		{
			var displaySettings = new DisplaySetting {
				Display = displayNumber,
				NumChars = numChars,
				NumLines = numLines,
				SegmentType = type,
				StyleDefinition = _styleDef
			};
			var display = new VirtualAlphaNumericDisplay(displaySettings, _config, ToggleSettings);

			if (_config != null && _config.HasGameName) {
				display.PositionChanged.Throttle(TimeSpan.FromMilliseconds(500)).Subscribe(position => {
					Logger.Info("Position changed: {0}", position);
					(_config.VirtualAlphaNumericDisplay as VirtualAlphaNumericDisplayConfig).SetPosition(displayNumber, position);
				});
			}

			_displays[displayNumber] = display;

			return display;
		}

		private void ToggleSettings(int displayNumber)
		{
			if (_settingWindow == null) {
				var window = _displays[displayNumber];
				_settingWindow = new VirtualAlphaNumericSettings(_styleDef, window.Top, window.Left + window.Width, _config);
				_settingWindow.IsVisibleChanged += (visibleSender, visibleEvent) => _settingsOpen = (bool)visibleEvent.NewValue;
				_settingSubscription = _settingWindow.OnStyleApplied.Subscribe(style => {
					Logger.Info("Applying new style to displays.");
					foreach (var d in _displays.Keys) {
						var display = _displays[d];
						display.AlphaNumericDisplay.UpdateStyle(style);
					}
				});
			}

			if (!_settingsOpen) {
				_settingWindow.Show();
			} else {
				_settingWindow.Hide();
			}
		}
	}

}
