using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using LibDmd.Common;
using LibDmd.Converter.Colorize;
using LibDmd.Frame;
using LibDmd.Input;
using NLog;

namespace LibDmd.Converter
{
	public class Gray2Colorizer : AbstractSource, IConverter, IColoredGray2Source, IColoredGray4Source
	{
		public override string Name { get; } = "2-Bit Colorizer";
		public FrameFormat From { get; } = FrameFormat.Gray2;
		public IObservable<Unit> OnResume { get; }
		public IObservable<Unit> OnPause { get; }
		public bool Has128x32Animation { get; private set; }

		private readonly Subject<ColoredFrame> _coloredGray2AnimationFrames = new Subject<ColoredFrame>();
		private readonly Subject<ColoredFrame> _coloredGray4AnimationFrames = new Subject<ColoredFrame>();

		/// <summary>
		/// Datä vomer uism .pal-Feil uisägläsä hend
		/// </summary>
		private readonly Coloring _coloring;

		/// <summary>
		/// Datä vomer uism Animazionsfeil uisägläsä hend
		/// </summary>
		private readonly AnimationSet _animations;

		/// <summary>
		/// Wenn nid `null` de isch das d Animazion wo grad ablaift
		/// </summary>
		private Animation _activeAnimation;

		/// <summary>
		/// Die etzigi Palettä
		/// </summary>
		private Palette _palette;

		/// <summary>
		/// Dr aktueui Palettä-Index. Da wird bruicht damit s PIN2DMD schnäuer cha
		/// Palettä wächslä, wuis d Palettä a sich schon hett und nur nu än Index
		/// bruicht.
		/// </summary>
		private int _paletteIndex;

		/// <summary>
		/// D Standardpalettä wo bruicht wird wenn grad nid erkennt wordä isch
		/// </summary>
		private Palette _defaultPalette;

		/// <summary>
		/// Dr Index wo ufd Standardpalettä zeigt
		/// </summary>
		private int _defaultPaletteIndex;

		/// <summary>
		/// Dr Timer wo bimänä ziitbeschränktä Palettäwächsu uifd Standardpalettä zruggsetzt
		/// </summary>
		private IDisposable _paletteReset;

		protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public Gray2Colorizer(Coloring coloring, AnimationSet animations)
		{
			_coloring = coloring;
			_animations = animations;
			Has128x32Animation = (_coloring.Masks != null && _coloring.Masks.Length >= 1 && _coloring.Masks[0].Length == 512);
			SetPalette(coloring.DefaultPalette, coloring.DefaultPaletteIndex, true);
		}

		public void Init()
		{
		}

		public void Convert(DmdFrame frame)
		{
			var planes = FrameUtil.Split(Dimensions.Value, 2, frame.Data);

			if (_coloring.Mappings != null)
			{
				if (frame is RawFrame vd && vd.RawPlanes.Length > 0)
				{
					// Reverse bit order for non-WPC.
					TriggerAnimation(vd.RawPlanes, vd.RawPlanes.Length > 3);
				}
				else
				{
					TriggerAnimation(planes, false);
				}
			}

			// Wenn än Animazion am laifä isch de wirds Frame dr Animazion zuägschpiut wos Resultat de säubr uisäschickt
			if (_activeAnimation != null) {
				_activeAnimation.NextFrame(frame.Dimensions, planes, AnimationFinished);
				return;
			}

			// Sisch diräkt uisgäh
			Render(frame.Dimensions, planes);
		}

		/// <summary>
		/// Tuät s Biud durähäschä, luägt obs än Animazion uisleest odr Palettä setzt und macht das grad.
		/// </summary>
		/// <param name="planes">S Buid zum iberpriäfä
		/// </param>
		///
		public void ActivateMapping(Mapping mapping)
		{
			if (mapping.Mode == SwitchMode.Event)
			{
				return;
			}

			// If same LCM scene, no need to stop/start
			if (_activeAnimation != null && _activeAnimation.SwitchMode == SwitchMode.LayeredColorMask && mapping.Mode == SwitchMode.LayeredColorMask && mapping.Offset == _activeAnimation.Offset)
			{
				return;
			}

			// Faus scho eppis am laifä isch, ahautä
			_activeAnimation?.Stop();
			_activeAnimation = null;

			// Palettä ladä
			var palette = _coloring.GetPalette(mapping.PaletteIndex);
			if (palette == null)
			{
				Logger.Warn("[colorize] No palette found at index {0}.", mapping.PaletteIndex);
				return;
			}
			//Logger.Debug("[colorize] Setting palette {0} of {1} colors.", mapping.PaletteIndex, palette.Colors.Length);
			_paletteReset?.Dispose();
			_paletteReset = null;
			SetPalette(palette, mapping.PaletteIndex);

			// Palettä risettä wenn ä Lengi gäh isch
			if (!mapping.IsAnimation && mapping.Duration > 0)
			{
				_paletteReset = Observable
					.Never<Unit>()
					.StartWith(Unit.Default)
					.Delay(TimeSpan.FromMilliseconds(mapping.Duration)).Subscribe(_ =>
					{
						if (_defaultPalette != null)
						{
							Logger.Debug("[colorize] Resetting to default palette after {0} ms.", mapping.Duration);
							SetPalette(_defaultPalette, _defaultPaletteIndex);
						}
						_paletteReset = null;
					});
			}

			// Animazionä
			if (mapping.IsAnimation)
			{

				// Luägä ob ibrhaipt äs VNI/FSQ Feil umä gsi isch
				if (_animations == null)
				{
					Logger.Warn("[colorize] Tried to load animation but no animation file loaded.");
					return;
				}
				_activeAnimation = _animations.Find(mapping.Offset);

				if (_activeAnimation == null)
				{
					Logger.Warn("[colorize] Cannot find animation at position {0}.", mapping.Offset);
					return;
				}

				_activeAnimation.Start(mapping.Mode, Render, AnimationFinished);
			}
		}

		private void TriggerAnimation(byte[][] planes, bool reverse)
		{
			uint nomaskcrc = 0;

			for (var i = 0; i < planes.Length; i++)
			{
				var mapping = FindMapping(planes[i], reverse, out nomaskcrc);

				// Faus niid gfundä hemmr fertig
				if (mapping != null)
				{
					Logger.Debug("[vni]Detect hash {0:X} for mode {1}", mapping.Checksum, mapping.Mode);

					ActivateMapping(mapping);
					// Can exit if not LCM sceene.
					if (_activeAnimation != null && _activeAnimation.SwitchMode != SwitchMode.LayeredColorMask)
						return;

				}
				if (_activeAnimation != null)
				{
					if (_activeAnimation.SwitchMode == SwitchMode.LayeredColorMask)
						_activeAnimation.DetectLCM(planes[i], nomaskcrc, reverse);
					else if (_activeAnimation.SwitchMode == SwitchMode.Follow || _activeAnimation.SwitchMode == SwitchMode.FollowReplace)
						_activeAnimation.DetectFollow(planes[i], nomaskcrc, reverse);
				}
			}
		}

		/// <summary>
		/// Tuät Bitplane fir Bitplane häschä unds erschtä Mäpping wo gfundä
		/// wordä isch zrugg gäh.
		/// </summary>
		/// <param name="planes">Bitplanes vom Biud</param>
		/// <returns>Mäpping odr null wenn nid gfundä</returns>
		private Mapping FindMapping(byte[] plane, bool reverse, out uint NoMaskCRC)
		{
			NoMaskCRC = 0;
			var maskSize = Dimensions.Value.Width * Dimensions.Value.Height / 8;

			var checksum = FrameUtil.Checksum(plane, reverse);

			NoMaskCRC = checksum;

			var mapping = _coloring.FindMapping(checksum);
			if (mapping != null) {
				return mapping;
			}

			// Wenn kä Maskä definiert, de nächschti Bitplane
			if (_coloring.Masks == null || _coloring.Masks.Length <= 0)
				return null;

			// Sisch gemmr Maskä fir Maskä durä und luägid ob da eppis passt
			var maskedPlane = new byte[maskSize];
			foreach (var mask in _coloring.Masks) {
				checksum = FrameUtil.ChecksumWithMask(plane, mask, reverse);
				mapping = _coloring.FindMapping(checksum);
				if (mapping != null) {
					return mapping;
				}
			}

			return null;
		}

		/// <summary>
		/// Tuäts Biud uif diä entschprächändä Sourcä uisgäh.
		/// </summary>
		/// <param name="dim">Grehssi</param>
		/// <param name="planes">S Biud zum uisgäh</param>
		private void Render(Dimensions dim, byte[][] planes)
		{
			// Wenns kä Erwiiterig gä hett, de gäbemer eifach d Planes mit dr Palettä zrugg
			if (planes.Length == 2) {
				_coloredGray2AnimationFrames.OnNext(new ColoredFrame(dim, planes, _palette.GetColors(planes.Length), _paletteIndex));
			}

			// Faus scho, de schickermr s Frame uifd entsprächendi Uisgab faus diä gsetzt isch
			if (planes.Length == 4) {
				_coloredGray4AnimationFrames.OnNext(new ColoredFrame(dim, planes, _palette.GetColors(planes.Length), _paletteIndex));
			}
		}

		/// <summary>
		/// Tuät nii Farbä dr Palettä wo grad bruichd wird zuäwiisä.
		/// </summary>
		/// <param name="palette">Diä nii Palettä</param>
		/// <param name="index">Welä Index mr muäss setzä</param>
		/// <param name="isDefault"></param>
		public void SetPalette(Palette palette, int index, bool isDefault = false)
		{
			if (palette == null) {
				Logger.Warn("[colorize] Ignoring null palette.");
				return;
			}
			if (isDefault) {
				_defaultPalette = palette;
				_defaultPaletteIndex = index;
			}
			//Logger.Debug("[colorize] Setting new palette: [ {0} ]", string.Join(" ", palette.Colors.Select(c => c.ToString())));
			_palette = palette;
			_paletteIndex = index;
		}

		/// <summary>
		/// Wird uisgfiährt wenn än Animazion fertig isch, cha irgend ä Modus si.
		/// </summary>
		protected void AnimationFinished()
		{
			//Logger.Trace("[timing] Animation finished.");
			//LastChecksum = 0x0;
			SetPalette(_defaultPalette, _defaultPaletteIndex);
			_activeAnimation = null;
		}

		public IObservable<ColoredFrame> GetColoredGray2Frames()
		{
			return _coloredGray2AnimationFrames;
		}

		public IObservable<ColoredFrame> GetColoredGray4Frames()
		{
			return _coloredGray4AnimationFrames;
		}
	}
}
