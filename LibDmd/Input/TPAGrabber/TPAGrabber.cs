using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using LibDmd.Common;
using LibDmd.Frame;

namespace LibDmd.Input.TPAGrabber
{
	/// <summary>
	/// Polls for the Pinball Arcade DX11 process, grabs the DMD display from the memory.
	/// </summary>
	/// <remarks>
	/// Can be launched any time. Will wait with sending frames until Pinball Arcade DX11 is
	/// launched and stop sending when it exits.
	/// </remarks>
	public class TPAGrabber : MemoryGrabber<DmdFrame>, IGray4Source
	{
		public override string Name { get; } = "Pinball Arcade DX11";

		public IObservable<DmdFrame> GetGray4Frames()
		{
			return GetFrames();
		}

		// DMD Stuff
		private const int DMDWidth = 128;
		private const int DMDHeight = 32;
		private static readonly byte[] RawDMD = new byte[MemBlockSize];
		private byte[] _lastFrame;
		private static bool sternInit = false;
		private DmdFrame _dmdFrame = new DmdFrame(new Dimensions(DMDWidth, DMDHeight));

		private static HashSet<double> lums = new HashSet<double>();

		protected override DmdFrame CaptureDMD()
		{
		// Initialize a new writeable bitmap to receive DMD pixels.
			var frame = new byte[DMDWidth * DMDHeight];

			// Init DMD hack for Stern tables.
			InitSternDMD(_hProcess, false);

			// Check if a table is loaded..
			var tableLoaded = new byte[1];
			ReadProcessMemory(_hProcess, _gameStateAddr, tableLoaded, 1, IntPtr.Zero);

			// ..if not, return an empty frame (blank DMD).
			if (tableLoaded[0] == 0) {
				sternInit = false; // Reset Stern DMD hack state.
				return _dmdFrame.Update(frame);
			}

			// Table is loaded, reset Stern DMD hack.
			InitSternDMD(_hProcess, true);

			// Retrieve the DMD entrypoint from EAX registry (returned by our codecave).
			var eax = new byte[4];
			ReadProcessMemory(_hProcess, _codeCave, eax, 4, IntPtr.Zero);

			// Now we have our DMD location in memory + little hack to re-align the DMD block.
			var dmdOffset = B4ToPointer(eax) - 0x1F406;

			// Grab the whole raw DMD block from game's memory.
			ReadProcessMemory(_hProcess, dmdOffset, RawDMD, MemBlockSize + 2, IntPtr.Zero);

			// Check the DMD CRC flag, skip the frame if the value is incorrect.
			if (RawDMD[0] != 0x02) {
				return null;
			}

			// Used to parse pixel bytes of the DMD memory block, starting at 2 to skip the flag bytes.
			var rawPixelIndex = 2;

			var identical = true;

			// For each pixel on Y axis.
			for (var dmdY = 0; dmdY < DMDHeight; dmdY++) {

				// For each pixel on X axis.
				for (var dmdX = 0; dmdX < DMDWidth; dmdX++) {

					// RGB to BGR
					double hue, sat, lum;
					ColorUtil.RgbToHsl(RawDMD[rawPixelIndex], RawDMD[rawPixelIndex + 1], RawDMD[rawPixelIndex + 2], out hue, out sat, out lum);

					var pos = dmdY * DMDWidth + dmdX;

					byte pixel;
					// NoEx: how to check if it's stern?
					if (hasSpecialDMD(_hProcess)) {
						// from STERN, we get: [ 0, 0.0666666666666667, 0.135294117647059, 0.203921568627451, 0.272549019607843, 0.341176470588235, 0.409803921568627, 0.47843137254902 ]
						pixel = (byte)(lum * 15 * 2);
					} else {
						// from others, we get: [ 0, 0.366666666666667, 0.509803921568627, 0.605882352941176 ]
						pixel = (byte)(lum * 15 * 1.6);
					}

					if (pixel > 0 && pixel < 15) {
						pixel++;
					}

					if (!lums.Contains(lum)) {
						lums.Add(lum);
						Logger.Trace(String.Join(" ", lums.ToArray().OrderBy(l => l)));
					}

					// drop garbage frames
					if (pixel > 15) {
						return null;
					}

					if (identical && (_lastFrame == null || _lastFrame[pos] == pixel)) {
						identical = false;
					}
					frame[pos] = pixel;

					// Each pixel takes 4 bytes of data in memory, advance 2 pixels.
					rawPixelIndex += 8;
				}
				// Jump to the next DMD line.
				rawPixelIndex += LineJump * 2 + DMDWidth * 8;
			}
			_lastFrame = frame;

			// Return the DMD bitmap we've created or null if frame was identical to previous.
			return identical ? null : _dmdFrame.Update(frame);
		}

		// try attaching to a process
		protected override IntPtr AttachGameProcess(Process p)
		{
			// check the process name
			if ((p.ProcessName == "PinballArcade11" || p.ProcessName == "PinballArcadeCabinet") && FindOffsets(p)) {
				// write the codecave
				var processHandle = PatchCodeCave(p);

				// success
				return processHandle;
			}

			// not our process
			return IntPtr.Zero;
		}

		// Codecave parameters
		private const int LineJump = 0x400;
		private const int MemBlockSize = 0x1FC02;

		// addresses in the target process
		private static IntPtr _dmdPatchAddr = IntPtr.Zero;
		private static IntPtr _gameStateAddr = IntPtr.Zero;
		private static IntPtr _dmdSternAddr = IntPtr.Zero;
		private static IntPtr _tableNameAddr = IntPtr.Zero;
		private static IntPtr _codeCave = IntPtr.Zero;

		// Not fully commented.. basically we're creating a codecave to let the game retrieve for us the DMD location in memory.
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static IntPtr PatchCodeCave(Process gameProc)
		{
			// Open the process for wait, read, and write operations
			var processHandle = OpenProcess(SYNCHRONIZE | PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, gameProc.Id);
			if (processHandle == IntPtr.Zero) {
				return processHandle;
			}

			// Allocating memory to write the codecave.
			_codeCave = VirtualAllocEx(processHandle, IntPtr.Zero, 0x100, 0x1000, 0x40);
			var mem = BitConverter.GetBytes((int)_codeCave);

			// Build the JMP to the original code.
			var joc = ASMJump(_codeCave + 17, _dmdPatchAddr + 13);

			// Build the codecave.
			var dmdCodeCave = new byte[] {
				0x0F, 0xB6, 0x16,                     // MOVZX EDX,BYTE PTR DS:[ESI]
				0x8B, 0x75, 0xF0,                     // MOV ESI,DWORD PTR SS:[EBP-10]
				0xD3, 0xEA,                           // SHR EDX,CL
				0x83, 0xE2, 0x01,                     // AND EDX,1
				0x03, 0xFA,                           // ADD EDI,EDX
				0xE9, joc[1], joc[2], joc[3], joc[4]  // JMP > ORIGINAL CODE
			};

			// Write the codecave into memory.
			WriteProcessMemory(processHandle, _codeCave + 4, dmdCodeCave, dmdCodeCave.Length, IntPtr.Zero);

			// Build the JMP to the codecave.
			var jcc = ASMJump(_dmdPatchAddr + 5, _codeCave + 4);

			// Build the jump to the codecave.
			var jmpToCC = new byte[] {
				0xA3, mem[0], mem[1], mem[2], mem[3], // MOV DWORD PTR DS:[XXXXXXXX],EAX
				0xE9, jcc[1], jcc[2], jcc[3], jcc[4], // JMP > CODECAVE
				0x90, 0x90, 0x90                      // NOP NOP NOP
			};
			// Write the jump into memory.
			WriteProcessMemory(processHandle, _dmdPatchAddr, jmpToCC, jmpToCC.Length, IntPtr.Zero);

			// Return the process handle.
			return processHandle;
		}

		// Function to calculate the 5-byte array for an ASM JMP.
		private static byte[] ASMJump(IntPtr location, IntPtr destination)
		{
			var JMPbytes = new byte[4];
			JMPbytes = BitConverter.GetBytes((UInt64)destination - (UInt64)location - (UInt64)5);
			return new byte[] { 0xE9, JMPbytes[0], JMPbytes[1], JMPbytes[2], JMPbytes[3] };
		}

		// Stern DMD Memory hack function.
		private static void InitSternDMD(IntPtr procHandle, bool reset)
		{
			// Stern DMD hack patch bytes.
			var sternPatch = new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00 };
			// Stern DMD original code.
			var sternOrig = new byte[] { 0x83, 0xF8, 0x4F, 0x74, 0x4B };
			// If the hack has not been initialized yet..
			if (!sternInit) {
				if (!reset)
					// Write the hack into memory.
					WriteProcessMemory(procHandle, _dmdSternAddr, sternPatch, sternPatch.Length, IntPtr.Zero);
				else {
					// Reset the code to its initial state.
					WriteProcessMemory(procHandle, _dmdSternAddr, sternOrig, sternOrig.Length, IntPtr.Zero);
					// Ready!
					sternInit = true;
				}
			}
		}

		private static bool hasSpecialDMD(IntPtr procHandle)
		{
			var tableBuffer = new byte[20];
			ReadProcessMemory(procHandle, _tableNameAddr, tableBuffer, tableBuffer.Length, IntPtr.Zero);
			var rawTableName = Encoding.Default.GetString(tableBuffer);
			if (rawTableName.IndexOf("_") < 0) {
				return false;
			}
			var tableName = rawTableName.Substring(1, rawTableName.IndexOf("_") - 1);
			var specialDMDTables = new string[] { "ACDC", "GhostBustersSter", "Mustang", "StarTrek" };
			return specialDMDTables.Any(tableName.Contains) ? true : false;
		}

		// byte patterns to find DMD structs in the target process
		private static readonly byte[] DMDCreationSignature = new byte[] { 0x8B, 0x5D, 0x10, 0x8D, 0x40, 0x08, 0x83, 0xE2, 0x01, 0x83, 0xE7, 0x01, 0x03, 0xFA };
		private static readonly byte[] GameStateSignature = new byte[] { 0xC7, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xC7, 0x87, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x74, 0x14, 0xC7, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00, 0xC7, 0x05 };
		private static readonly byte[] SternDMDSignature = new byte[] { 0x75, 0x0A, 0xBA, 0x3E, 0x01, 0x00, 0x00, 0xB9, 0x3E, 0x00, 0x00, 0x00, 0x8B, 0x86, 0xD8, 0x29, 0x06, 0x00 };
		private static readonly byte[] TableNameSignature = new byte[] { 0x85, 0xC0, 0x74, 0x08, 0xC7, 0x00, 0x00, 0x00, 0x00, 0x00, 0xEB, 0x02, 0x33, 0xC0, 0x51, 0xBA, 0xFF, 0xFF, 0xFF, 0xFF, 0xB9 };

		private static bool FindOffsets(Process gameProc)
		{
			// Get game process base address
			var gameBase = BaseAddress(gameProc);

			// Find DMD creation location in memory by looking for the signature pattern
			_dmdPatchAddr = FindPattern(gameProc, gameBase, gameProc.MainModule.ModuleMemorySize, DMDCreationSignature, 14);
			if (_dmdPatchAddr == IntPtr.Zero) {
				return false;
			}

			// Find Stern DMD handling location in memory by looking for the signature pattern
			_dmdSternAddr = FindPattern(gameProc, gameBase, gameProc.MainModule.ModuleMemorySize, SternDMDSignature, 18);
			if (_dmdSternAddr == IntPtr.Zero) {
				return false;
			}

			// Retrieve table name pointer + offset
			var tableNamePointer = FindPattern(gameProc, gameBase, gameProc.MainModule.ModuleMemorySize, TableNameSignature, 21);
			var tableBuf = new byte[4];
			ReadProcessMemory(gameProc.Handle, tableNamePointer, tableBuf, tableBuf.Length, IntPtr.Zero);
			_tableNameAddr = B4ToPointer(tableBuf);

			// Retrieve game state pointer + offset
			var gameStatePointer = FindPattern(gameProc, gameBase, gameProc.MainModule.ModuleMemorySize, GameStateSignature, 34);
			var stateBuf = new byte[4];
			ReadProcessMemory(gameProc.Handle, gameStatePointer, stateBuf, stateBuf.Length, IntPtr.Zero);
			_gameStateAddr = B4ToPointer(stateBuf);

			// success
			return true;
		}
	}
}
