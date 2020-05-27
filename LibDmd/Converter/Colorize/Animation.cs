using System;
using System.Windows.Media;
using System.Collections.Generic;
using LibDmd.Common;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Converter.Colorize
{
	public abstract class Animation
	{
		public string Name { get; protected set; }

		/// <summary>
		/// Wefu Biudr diä Animazion het
		/// </summary>
		public int NumFrames => Frames.Length;

		/// <summary>
		/// Bitlängi odr Ahzahl Planes vo dr Buidr vo dr Animazion
		/// </summary>
		public int BitLength => Frames.Length > 0 ? Frames[0].BitLength : 0;

		/// <summary>
		/// Uif welärä Posizion (i Bytes) d Animazion im Feil gsi isch
		/// </summary>
		///
		/// <remarks>
		/// Wird aus Index zum Ladä bruicht.
		/// </remarks>
		public readonly long Offset;

		/// <summary>
		/// D Biudr vo dr Animazion
		/// </summary>
		protected AnimationFrame[] Frames;

		/// <summary>
		/// D Lengi vo dr ganzä Animazio i Millisekundä
		/// </summary>
		protected uint AnimationDuration;

		#region Unused Props
		protected int Cycles;
		protected int Hold;
		protected int ClockFrom;
		protected bool ClockSmall;
		protected bool ClockInFront;
		protected int ClockOffsetX;
		protected int ClockOffsetY;
		protected int RefreshDelay;
		[Obsolete] protected int Type;
		protected int Fsk;

		protected int PaletteIndex;
		protected Color[] AnimationColors;
		protected AnimationEditMode EditMode;
		protected int TransitionFrom;

		#endregion
		protected int Width;
		protected int Height;


		#region Animation-related

		/// <summary>
		/// Wiä langs nu gaht bisd Animazion fertig isch
		/// </summary>
		public int RemainingFrames => NumFrames - _frameIndex;

		/// <summary>
		/// Faus ja de wärdid Biudr ergänzt, sisch wärdits uistuischt
		/// </summary>
		public bool AddPlanes => SwitchMode == SwitchMode.Follow || SwitchMode == SwitchMode.ColorMask;

		/// <summary>
		/// Dr Modus vo dr Animazion wo bestimmt wiäd Biudr aagwänded wärdid
		/// </summary>
		public SwitchMode SwitchMode { get; private set; }

		/// <summary>
		/// Zeigt ah obd Animazion nu am laifä isch
		/// </summary>
		public bool IsRunning { get; private set; }

		/// <summary>
		/// Set by matching routine if follow mask is found
		/// </summary>
		public bool FoundFollowMatch { get; set; }

		/// <summary>
		/// Current frame's following mask
		/// </summary>
		public byte[] FollowMask { get; private set; }
		public uint FollowHash { get; private set; }

		private IObservable<AnimationFrame> _frames;
		private Action<Dimensions, byte[][]> _currentRender;
		private int _lastTick;
		private int _timer;

		/// <summary>
		/// Index of the frame currently displaying (or enhancing).
		/// </summary>
		private int _frameIndex;

		#endregion

		/// <summary>
		/// Next hash to look for (in col seq mode)
		/// </summary>
		uint Crc32 { get; }



		/// <summary>
		/// Mask for "Follow" switch mode.
		/// </summary>
		byte[] Mask { get; }

		protected byte[][] Masks;

		private List<byte[]> LCMBufferPlanes = new List<byte[]>();

		protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected Animation(long offset)
		{
			Offset = offset;
		}

		/// <summary>
		/// Tuät d Animazion startä.
		/// </summary>
		///
		/// <param name="mode">Dr Modus i welem d Animazion laift (chunnt uifs Mappind druif ah)</param>
		/// <param name="render">Ä Funktion wo tuät s Buid uisgäh</param>
		/// <param name="completed">Wird uisgfiärt wenn fertig</param>
		public void Start(SwitchMode mode, Action<Dimensions, byte[][]> render, Action completed = null)
		{
			IsRunning = true;
			SwitchMode = mode;
			_frameIndex = 0;
			FoundFollowMatch = true; // Always render the first frame in a follow sequence.

			switch (SwitchMode)
			{
				case SwitchMode.ColorMask:
				case SwitchMode.Follow:
				case SwitchMode.MaskedReplace:
					StartEnhance(render);
					break;
				case SwitchMode.Replace:
				case SwitchMode.FollowReplace:
					StartReplace(render, completed);
					break;
				case SwitchMode.LayeredColorMask:
					StartLCM(render);
					break;
			}
		}

		private void StartEnhance(Action<Dimensions, byte[][]> render)
		{
			_lastTick = Environment.TickCount;
			_timer = 0;
			_currentRender = render;
			InitializeFrame();

			if (NumFrames == 1) {
				Logger.Debug("[vni][{0}] Enhancing single frame, duration = {1}ms ({2})...", SwitchMode, AnimationDuration, Name);
				_timer = (int)AnimationDuration;
			} else {
				Logger.Debug("[vni][{0}] Starting enhanced animation of {1} frame{2} ({3})...", SwitchMode, NumFrames, NumFrames == 1 ? "" : "s", Name);
			}
		}

		private void ClearLCMBuffer()
		{
			LCMBufferPlanes.ForEach(p => Common.FrameUtil.ClearPlane(p));
		}

		private void RenderLCM(Dimensions dim, byte[][] vpmFrame)
		{
			_currentRender(dim, new[] { vpmFrame[0], vpmFrame[1], LCMBufferPlanes[2], LCMBufferPlanes[3] });
		}

		public void DetectFollow(byte[] plane, uint NoMaskCRC, bool Reverse)
		{
			if (NoMaskCRC == FollowHash || FrameUtil.ChecksumWithMask(plane, FollowMask, Reverse) == FollowHash)
				FoundFollowMatch = true;
		}

		public void DetectLCM(byte[] plane, uint NoMaskCRC, bool Reverse)
		{
			bool clear = true;
			uint checksum = NoMaskCRC;
			var maskSize = Width * Height / 8;
			var maskedPlane = new byte[maskSize];

			for (int k = -1; k < Masks.Length; k++)
			{
				if (k >= 0)
				{
					checksum = FrameUtil.ChecksumWithMask(plane, Masks[k], Reverse);
				}
				foreach (var af in Frames)
				{
					if (af.Hash == checksum)
					{
						if (clear)
						{
							ClearLCMBuffer();
							clear = false;
						}
						for (int i = 0; i < af.Planes.Count; i++)
						{
							FrameUtil.OrPlane(af.PlaneData[i], LCMBufferPlanes[i]);
						}
					}

				}
			}
		}

		private void StartLCM(Action<Dimensions, byte[][]> render)
		{
			_currentRender = render;
			LCMBufferPlanes.Clear();
			for (int i = 0; i < Frames[0].Planes.Count; i++)
				LCMBufferPlanes.Add(LibDmd.Common.FrameUtil.NewPlane(Width, Height));

			ClearLCMBuffer();

			Logger.Debug("[vni][{0}] Started LCM mode, ({1})...", SwitchMode, Name);
		}

		private void StartReplace(Action<Dimensions, byte[][]> render, Action completed = null)
		{
			Logger.Debug("[vni][{0}] Starting colored animation of {1} frames ({2})...", SwitchMode, Frames.Length, Name);
			_lastTick = Environment.TickCount;
			_timer = 0;
			_currentRender = render;
			InitializeFrame();
		}

		private void RenderAnimation(Dimensions dim, byte[][] vpmFrame, Action completed = null)
		{
			if (SwitchMode == SwitchMode.ColorMask || SwitchMode == SwitchMode.Replace || SwitchMode == SwitchMode.MaskedReplace)
			{
				var delay = Environment.TickCount - _lastTick;
				_lastTick = Environment.TickCount;

				_timer -= delay;
				if (_frameIndex >= NumFrames)
				{
					Logger.Error("[vni][{0}] No more frames in animation ({1}).", SwitchMode, NumFrames);
					return;
				}
			}
			if (!(SwitchMode == SwitchMode.Follow || SwitchMode == SwitchMode.FollowReplace) || FoundFollowMatch)
			{
				OutputFrame(dim, vpmFrame);
			}
			// Advance frames - when not in LCM, AND
			// When timer runs out and not in follow modes OR
			// When in follow modes and a match is detected.
			if (SwitchMode != SwitchMode.LayeredColorMask &&
				 ((_timer <= 0 && !(SwitchMode == SwitchMode.Follow || SwitchMode == SwitchMode.FollowReplace))
				 || ((SwitchMode == SwitchMode.Follow || SwitchMode == SwitchMode.FollowReplace) && FoundFollowMatch)))
			{
				_frameIndex++;
				FoundFollowMatch = false;
				if (_frameIndex == NumFrames)
				{
					// If it's a follow mode the final frame should repeat indefinitely, only interrupted by another hash match elsewhere
					if (SwitchMode == SwitchMode.Follow || SwitchMode == SwitchMode.FollowReplace)
					{
						_frameIndex--;
					}
					else
					{
						Stop("finished");
						completed?.Invoke();
					}
				}
				else
				{
					InitializeFrame();
				}
			}
		}

		private void OutputFrame(Dimensions dim, byte[][] vpmFrame)
		{
			switch (SwitchMode)
			{
				case SwitchMode.ColorMask:
				case SwitchMode.Follow:
					RenderColorMask(dim, vpmFrame);
					break;
				case SwitchMode.FollowReplace:
				case SwitchMode.Replace:
				case SwitchMode.MaskedReplace:
					byte[][] outplanes;
					var animplanes = Frames[_frameIndex].PlaneData;

					if (SwitchMode != SwitchMode.MaskedReplace)
					{
						outplanes = animplanes;
					}
					else
					{
						var planecount = animplanes.Length;
						outplanes = new byte[planecount][];
						for (int i = 0; i < planecount; i++)
						{
							if (i < vpmFrame.Length)
								outplanes[i] = FrameUtil.CombinePlaneWithMask(animplanes[i], vpmFrame[i], FollowMask);
							else
								outplanes[i] = animplanes[i];
						}
					}
					_currentRender(dim, outplanes);
					break;
				case SwitchMode.LayeredColorMask:
					RenderLCM(dim, vpmFrame);
					break;
			}
		}

		private void RenderColorMask(Dimensions dim, byte[][] vpmFrame)
		{
			if (Frames[_frameIndex].Planes.Count < 4)
			{
				Logger.Warn("[vni][{0}] Cannot enhance frame with {1} additional bitplanes.", SwitchMode, Frames[_frameIndex].Planes.Count);
			}
			else
			{
				_currentRender(dim, new[] { vpmFrame[0], vpmFrame[1], Frames[_frameIndex].Planes[2].Plane, Frames[_frameIndex].Planes[3].Plane });
			}
		}

		private void InitializeFrame()
		{
			_timer += (int)Frames[_frameIndex].Delay;
			if (SwitchMode == SwitchMode.Follow || SwitchMode == SwitchMode.FollowReplace)
			{
				// Need the *next* frame's mask and hash
				if (_frameIndex + 1 < NumFrames)
				{
					FollowHash = Frames[_frameIndex+1].Hash;
					FollowMask = Frames[_frameIndex+1].Mask;
				}
			}
			else if (SwitchMode == SwitchMode.MaskedReplace)
			{
				// Need the mask from this frame.
				FollowMask = Frames[_frameIndex].Mask;
			}
		}


		public void NextFrame(Dimensions dim, byte[][] planes, Action completed = null)
		{
			RenderAnimation(dim, planes, completed);
		}

		public void Stop(string what = "stopped")
		{
			_frameIndex = 0;
			_currentRender = null;
			IsRunning = false;
		}

		public bool Equals(Animation animation)
		{
			return Offset == animation.Offset;
		}

		public override string ToString()
		{
			return $"{Name}, {Frames.Length} frames";
		}
	}

	public enum AnimationEditMode
	{
		Replace, Mask, Fixed
	}
}
