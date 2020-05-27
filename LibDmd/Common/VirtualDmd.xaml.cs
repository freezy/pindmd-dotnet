using System.Windows;
using System.Windows.Controls;
using LibDmd.DmdDevice;

namespace LibDmd.Common
{
	/// <summary>
	/// A borderless virtual DMD that resizes with the same aspect ratio (if not disabled)
	/// </summary>
	public partial class VirtualDmd : VirtualDisplay
	{
		public override IVirtualControl VirtualControl => Dmd;

		private readonly VirtualDmdConfig _config;
		private readonly MenuItem _saveGamePosItem;

		public double DotSize
		{
			set
			{
				if (Dmd != null) {
					Dmd.DotSize = value;
				}
			}
		}

		public VirtualDmd(VirtualDmdConfig config = null, string gameName = null) : base()
		{
			InitializeComponent();
			Initialize();
			if (config != null) {
				_config = config;

				ParentGrid.ContextMenu = new ContextMenu();

				var saveGlobalPos = new MenuItem();
				saveGlobalPos.Click += SavePositionGlobally;
				saveGlobalPos.Header = "Save position globally";
				ParentGrid.ContextMenu.Items.Add(saveGlobalPos);

				if (gameName != null) {
					_saveGamePosItem = new MenuItem();
					_saveGamePosItem.Click += SavePositionGame;
					_saveGamePosItem.Header = "Save position for \"" + gameName + "\"";
					ParentGrid.ContextMenu.Items.Add(_saveGamePosItem);
				}

				ParentGrid.ContextMenu.Items.Add(new Separator());
				
				var toggleAspect = new MenuItem();
				toggleAspect.Click += ToggleAspectRatio;
				toggleAspect.Header = "Ignore Aspect Ratio";
				toggleAspect.IsCheckable = true;
				ParentGrid.ContextMenu.Items.Add(toggleAspect);

			}
		}

		private void SavePositionGlobally(object sender, RoutedEventArgs e)
		{
			_config.SetPosition(new VirtualDisplayPosition(Left, Top, Width, Height), false);
		}

		private void SavePositionGame(object sender, RoutedEventArgs e)
		{
			_config.SetPosition(new VirtualDisplayPosition(Left, Top, Width, Height), true);
		}

		private void ToggleAspectRatio(object sender, RoutedEventArgs e)
		{
			IgnoreAspectRatio = (sender as MenuItem).IsChecked;
			_config.SetIgnoreAspectRatio(IgnoreAspectRatio);
		}

		internal void SetGameName(string gameName)
		{
			if (_saveGamePosItem != null) {
				_saveGamePosItem.Header = "Save position for \"" + gameName + "\"";
			}
		}
	}
}
