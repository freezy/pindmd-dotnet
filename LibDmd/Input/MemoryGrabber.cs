using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;

namespace LibDmd.Input
{
	public abstract class MemoryGrabber<FrameType> : MemoryGrabberBase
	{
		/// <summary>
		/// Determine if the given process is the subject process, and attach
		/// to it if so.  On success, this finds the DMD struct(s) within the target
		/// process, stores their addresses in subclass member variables as needed
		/// to read the DMD state on an ongoing basis, establishes any required
		/// hooks in the target process (e.g., the Pinball Arcade "code cave"), and
		/// returns the Win32 HANDLE of the open process.  If the process isn't the
		/// subject process, or the DMD struct(s) can't be located, returns null
		/// (IntPtr.Zero).
		/// </summary>
		protected abstract IntPtr AttachGameProcess(Process p);

		/// <summary>
		/// Do the DMD capture from memory.  This is called once per frame, on
		/// the FPS timer.
		/// </summary>
		/// <returns></returns>
		protected abstract FrameType CaptureDMD();

		/// <summary>
		/// Wait time between polls for the subject process. Stops polling as soon
		/// as the process is found.
		///
		/// Can be set quite high, just about as long as it takes for subject to start.
		/// </summary>
		public TimeSpan PollForProcessDelay { get; set; } = TimeSpan.FromSeconds(10);

		/// <summary>
		/// Frequency with which frames are pulled off the memory.
		/// </summary>
		public double FramesPerSecond { get; set; } = 60;

		// logger
		protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		// capturer
		private IDisposable _capturer;

		// subject process handle
		protected IntPtr _hProcess;

		/// <summary>
		/// Search the system process list for the subject process.  Returns a Win32
		/// HANDLE to the process on success, with the memory grabber addresses stored
		/// and any "code cave" or other hack into the subject process space established.
		/// Returns IntPtr.Zero if no suitable subject process can be found.
		/// </summary>
		protected IntPtr FindGameHandle()
		{
			// search the process list
			foreach (var p in Process.GetProcesses())
			{
				// try this process
				var h = AttachGameProcess(p);

				// if that yielded a handle, use this proces
				if (h != IntPtr.Zero)
					return h;
			}

			// no matches
			return IntPtr.Zero;
		}

		/// <summary>
		/// Poll for the subject process to start
		/// </summary>
		/// 
		private void StartPolling()
		{
			// enable debug privileges to gain access to the target process's memory space
			SetDebugPrivilege();

			Logger.Info($"Waiting for {Name} process to start...");
			var success = new Subject<Unit>();
			Observable
				.Timer(TimeSpan.Zero, PollForProcessDelay)
				.TakeUntil(success)
				.Subscribe(x => {
					_hProcess = FindGameHandle();
					if (_hProcess != IntPtr.Zero)
					{
						Logger.Info($"Process found, starting capturing...");
						StartCapturing();
						success.OnNext(Unit.Default);
					}
				});
		}

		protected IConnectableObservable<FrameType> _framesObservable = null;
		public virtual IConnectableObservable<FrameType> GetFrames()
		{
			if (_framesObservable == null)
			{
				_framesObservable = Observable
					.Interval(TimeSpan.FromMilliseconds(1000 / FramesPerSecond))
					.Select(x => 
					{
						// if the process has exited, stop capture
						if (WaitForSingleObject(_hProcess, 0) == WAIT_OBJECT_0)
						{
							CloseHandle(_hProcess);
							_hProcess = IntPtr.Zero;
							StopCapturing();
							return default(FrameType);
						}

						return CaptureDMD();
					})
					.Where(frame => frame != null)
					.Publish();

				StartPolling();
			}

			return _framesObservable;
		}

		/// <summary>
		/// Starts sending frames.
		/// </summary>
		private void StartCapturing()
		{
			Logger.Info($"Reading DMD data from {Name}'s memory at {FramesPerSecond} fps...");
			_capturer = _framesObservable.Connect();
			_onResume.OnNext(Unit.Default);
		}

		/// <summary>
		/// Stops sending frames, usually because the subject process terminated.
		/// </summary>
		private void StopCapturing()
		{
			// TODO send blank frame
			Logger.Info($"Terminating DMD data capture from {Name}");
			_capturer.Dispose();
			_onPause.OnNext(Unit.Default);
			StartPolling();
		}

		// Reactive pause/resume subjects
		protected readonly ISubject<Unit> _onResume = new Subject<Unit>();
		protected readonly ISubject<Unit> _onPause = new Subject<Unit>();

		// Reactive pause/resume observables
		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		// Get the base address of a process's memory space
		protected static IntPtr BaseAddress(Process process)
		{
			var procMod = process.MainModule;
			return procMod.BaseAddress;
		}

		// Convert a four-byte buffer read from process memory to a 32-bit pointer
		protected static IntPtr B4ToPointer(byte[] buf)
		{
			return new IntPtr(BitConverter.ToInt32(buf, 0));
		}

		// Search a process's memory space for a byte pattern.  0xFF in the pattern
		// is a wildcard that matches any single byte.
		protected static IntPtr FindPattern(Process gameProc, IntPtr gameBaseAddr, int size, byte[] bytePattern, int offset)
		{
			// Create a byte array to store memory region.
			var memoryRegion = new byte[size];

			// Dump process memory into the array. 
			ReadProcessMemory(gameProc.Handle, gameBaseAddr, memoryRegion, size, IntPtr.Zero);

			// Loop into dumped memory region to find the pattern.
			for (var x = 0; x < memoryRegion.Length - bytePattern.Length; x++)
			{
				// If we find the first pattern's byte in memory, loop through the entire array.
				for (var y = 0; y < bytePattern.Length; y++)
				{

					// If pattern byte is 0xFF, this is a joker, continue pattern loop.
					if (bytePattern[y] == 0xFF)	{
						continue;
					}

					// If pattern byte is different than memory byte, we're not at the right place, back to the memory region loop...
					if (bytePattern[y] != memoryRegion[x + y]) {
						break;
					}

					// We've reached the end of the pattern array, we've found the offset.
					if (y == bytePattern.Length - 1) { 
						return gameBaseAddr + offset + x;
					}
				}
			}

			// We've reached the end of memory region, offset not found.
			return IntPtr.Zero;
		}

	}
}
