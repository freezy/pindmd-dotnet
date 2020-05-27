using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibDmd.DmdDevice;
using NLog;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace LibDmd.Output.Virtual.AlphaNumeric
{
	/// <summary>
	/// Interaction logic for AlphaNumericSettings.xaml
	/// </summary>
	public partial class VirtualAlphaNumericSettings
	{
		private static readonly AlphaNumericResources Res = AlphaNumericResources.GetInstance();
		protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static int Dpi = 96;

		public ISubject<RasterizeStyleDefinition> OnStyleApplied { get; } = new Subject<RasterizeStyleDefinition>();

		private readonly DisplaySetting _displaySetting;
		private readonly Configuration _config;
		private readonly RasterizeStyleDefinition _originalStyle;
		private readonly VirtualAlphaNumericDisplayConfig _alphaNumericConfig;
		private ushort[] _data = { };
		private readonly int[] _segments = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };

		public SegmentWeight SegmentWeight {
			get => _displaySetting.StyleDefinition.SegmentWeight;
			set => _displaySetting.StyleDefinition.SegmentWeight = value;
		}

		public List<string> StyleNames => _config == null ? null : _alphaNumericConfig.GetStyleNames();
		public string NewStyleName { get; set; }

		public VirtualAlphaNumericSettings(RasterizeStyleDefinition styleDefinition, double top, double left, Configuration config)
		{
			Top = top;
			Left = left;
			_config = config;
			_originalStyle = styleDefinition.Copy();
			_alphaNumericConfig = _config?.VirtualAlphaNumericDisplay as VirtualAlphaNumericDisplayConfig;
			_displaySetting = new DisplaySetting(
				100,
				SegmentType.Alphanumeric,
				styleDefinition.Copy(),
				10,
				1
			);

			DataContext = this;
			InitializeComponent();

			_displaySetting.SetDimensions((int)Preview.Width, (int)Preview.Height);

			PreviewText.TextChanged += PreviewTextChanged;
			PreviewText.Text = "  DMDEXT  ";
			if (config == null) {
				SaveGroup.Visibility = Visibility.Collapsed;
			}

			UpdateControls();
			SetupTriggers();

			Logger.Info("Creating preview image at {0}x{1}", (int)Preview.Width, (int)Preview.Height);
			var writeableBitmap = new WriteableBitmap((int)Preview.Width, (int)Preview.Height, Dpi, Dpi, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
			Preview.Source = writeableBitmap;

			CompositionTarget.Rendering += (o, e) => DrawPreview(writeableBitmap);

			// name each layer
			ForegroundStyle.Label = "Foreground Layer";
			InnerGlowStyle.Label = "Inner Glow Layer";
			OuterGlowStyle.Label = "Outer Glow Layer";
			BackgroundStyle.Label = "Unlit Layer";

			// subscribe to control changes that trigger rasterization
			ForegroundStyle.OnLayerChanged.DistinctUntilChanged().Subscribe(layerStyleDef => {
				_displaySetting.ApplyLayerStyle(RasterizeLayer.Foreground, layerStyleDef);
				Res.RasterizeLayer(_displaySetting, RasterizeLayer.Foreground, layerStyleDef, _displaySetting.Style.Foreground, _segments, _displaySetting.StyleDefinition.SkewAngle);
			});
			InnerGlowStyle.OnLayerChanged.DistinctUntilChanged().Subscribe(layerStyle => {
				_displaySetting.ApplyLayerStyle(RasterizeLayer.InnerGlow, layerStyle);
				Res.RasterizeLayer(_displaySetting, RasterizeLayer.InnerGlow, layerStyle, _displaySetting.Style.InnerGlow, _segments, _displaySetting.StyleDefinition.SkewAngle);
			});
			OuterGlowStyle.OnLayerChanged.DistinctUntilChanged().Subscribe(layerStyle => {
				_displaySetting.ApplyLayerStyle(RasterizeLayer.OuterGlow, layerStyle);
				Res.RasterizeLayer(_displaySetting, RasterizeLayer.OuterGlow, layerStyle, _displaySetting.Style.OuterGlow, _segments, _displaySetting.StyleDefinition.SkewAngle);
			});
			BackgroundStyle.OnLayerChanged.DistinctUntilChanged().Subscribe(layerStyle => {
				_displaySetting.ApplyLayerStyle(RasterizeLayer.Background, layerStyle);
				Res.RasterizeLayer(_displaySetting, RasterizeLayer.Background, layerStyle, _displaySetting.Style.Background, new [] { AlphaNumericResources.FullSegment }, _displaySetting.StyleDefinition.SkewAngle);
			});

			Res.Rasterize(_displaySetting, true);
		}

		private void SetupTriggers()
		{
			StyleNameComboBox.SelectionChanged += (sender, e) => {
				if (e.AddedItems.Count > 0) StyleSelectionChanged(e.AddedItems[0].ToString());
			};
			StyleNameComboBox.KeyUp += (sender, e) => StyleSelectionChanged(StyleNameComboBox.Text);

			SkewAngleSlider.ValueChanged += (sender, e) => SkewAngleValue.Text = DoubleToString(SkewAngleSlider.Value);
			SkewAngleValue.TextChanged += (sender, e) => SkewAngleSlider.Value = StringToDouble(SkewAngleValue.Text, SkewAngleSlider.Value);
			SkewAngleSlider.ValueChanged += (sender, e) => _displaySetting.StyleDefinition.SkewAngle = -(float)SkewAngleSlider.Value;
			SkewAngleSlider.ValueChanged += (sender, e) => RasterizeAll();
			ThinWeight.Checked += (sender, e) => RasterizeAll();
			BoldWeight.Checked += (sender, e) => RasterizeAll();
			BackgroundColor.SelectedColorChanged += (sender, e) =>
				_displaySetting.StyleDefinition.BackgroundColor = new SKColor(BackgroundColor.SelectedColor.Value.R,
					BackgroundColor.SelectedColor.Value.G, BackgroundColor.SelectedColor.Value.B,
					BackgroundColor.SelectedColor.Value.A);

		}

		private void UpdateControls()
		{
			SkewAngleValue.Text = (-_displaySetting.StyleDefinition.SkewAngle).ToString();
			SkewAngleSlider.Value = -_displaySetting.StyleDefinition.SkewAngle;
			BackgroundColor.SelectedColor = _displaySetting.StyleDefinition.BackgroundColor.ToColor();
			ThinWeight.IsChecked = _displaySetting.StyleDefinition.SegmentWeight == SegmentWeight.Thin;
			BoldWeight.IsChecked = _displaySetting.StyleDefinition.SegmentWeight == SegmentWeight.Bold;

			ForegroundStyle.RasterizeStyleDefinition = _displaySetting.StyleDefinition.Foreground;
			InnerGlowStyle.RasterizeStyleDefinition = _displaySetting.StyleDefinition.InnerGlow;
			OuterGlowStyle.RasterizeStyleDefinition = _displaySetting.StyleDefinition.OuterGlow;
			BackgroundStyle.RasterizeStyleDefinition = _displaySetting.StyleDefinition.Background;
		}

		private void RasterizeAll()
		{
			Res.RasterizeLayer(_displaySetting, RasterizeLayer.Foreground, _displaySetting.StyleDefinition.Foreground, _displaySetting.Style.Foreground, _segments, _displaySetting.StyleDefinition.SkewAngle);
			Res.RasterizeLayer(_displaySetting, RasterizeLayer.InnerGlow, _displaySetting.StyleDefinition.InnerGlow, _displaySetting.Style.InnerGlow, _segments, _displaySetting.StyleDefinition.SkewAngle);
			Res.RasterizeLayer(_displaySetting, RasterizeLayer.OuterGlow, _displaySetting.StyleDefinition.OuterGlow, _displaySetting.Style.OuterGlow, _segments, _displaySetting.StyleDefinition.SkewAngle);
			Res.RasterizeLayer(_displaySetting, RasterizeLayer.Background, _displaySetting.StyleDefinition.Background, _displaySetting.Style.Background, new[] { AlphaNumericResources.FullSegment }, _displaySetting.StyleDefinition.SkewAngle);
		}

		private void PreviewTextChanged(object sender, TextChangedEventArgs e)
		{
			_data = AlphaNumericPainter.GenerateAlphaNumeric(PreviewText.Text);
		}

		private void DrawPreview(WriteableBitmap writeableBitmap)
		{
			var width = (int)writeableBitmap.Width;
			var height = (int)writeableBitmap.Height;

			writeableBitmap.Lock();

			var surfaceInfo = new SKImageInfo {
				Width = width,
				Height = height,
				ColorType = SKColorType.Bgra8888,
				AlphaType = SKAlphaType.Premul,
			};
			using (var surface = SKSurface.Create(surfaceInfo, writeableBitmap.BackBuffer, width * 4)) {
				AlphaNumericPainter.DrawDisplay(surface, _displaySetting, _data);
			}
			writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
			writeableBitmap.Unlock();
		}

		private void StyleSelectionChanged(string name)
		{
			Logger.Info("Selection changed to {0}", name);
			if (StyleNames.Contains(name)) {
				LoadStyleButton.IsEnabled = true;
				DeleteStyleButton.IsEnabled = true;
			} else {
				LoadStyleButton.IsEnabled = false;
				DeleteStyleButton.IsEnabled = false;
			}
		}

		private void SaveToIniClicked(object sender, RoutedEventArgs e)
		{
			Logger.Info("Saving style {0} to DmdDevice.ini...", StyleNameComboBox.Text);
			var styleName = NewStyleName ?? StyleNameComboBox.Text;
			_alphaNumericConfig.SetStyle(styleName, _displaySetting.StyleDefinition);
			StyleSelectionChanged(styleName);
			StyleNameComboBox.ItemsSource = StyleNames;
			StyleNameComboBox.SelectedItem = styleName;
		}

		private void LoadFromIniClicked(object sender, RoutedEventArgs e)
		{
			_displaySetting.StyleDefinition = _alphaNumericConfig.GetStyle(NewStyleName);
			RasterizeAll();
			UpdateControls();
		}

		private void DeleteFromIniClicked(object sender, RoutedEventArgs e)
		{
			_alphaNumericConfig.RemoveStyle(NewStyleName);
			StyleNameComboBox.ItemsSource = StyleNames;
			StyleNameComboBox.Text = "";
			StyleSelectionChanged("");
		}

		private void ApplyClicked(object sender, RoutedEventArgs e)
		{
			OnStyleApplied.OnNext(_displaySetting.StyleDefinition.Copy());
			if (NewStyleName != null) {
				_alphaNumericConfig.ApplyStyle(NewStyleName);
			}
		}

		private void ResetClicked(object sender, RoutedEventArgs e)
		{
			_displaySetting.ApplyStyle(_originalStyle.Copy());
			RasterizeAll();
			UpdateControls();
		}

		private void CancelClicked(object sender, RoutedEventArgs e)
		{
			Hide();
		}

		private static string DoubleToString(double d)
		{
			return ((int)Math.Round(d)).ToString();
		}

		private static double StringToDouble(string str, double fallback)
		{
			try {
				return double.Parse(str);
			} catch (Exception) {
				return fallback;
			}
		}
	}

	public class ComparisonConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value?.Equals(parameter);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value?.Equals(true) == true ? parameter : Binding.DoNothing;
		}
	}
}
