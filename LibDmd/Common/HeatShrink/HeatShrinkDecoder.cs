using System;
using System.Diagnostics;
using System.IO;

namespace LibDmd.Common.HeatShrink
{
	/// <summary>
	/// Implementation of the HeatShrink compression algorithm
	/// </summary>
	/// 
	/// <see cref="https://github.com/atomicobject/heatshrink/blob/master/heatshrink_decoder.c">Original Implementation</see>
	/// <see cref="https://github.com/sker65/heatshrink-java/blob/master/src/main/java/com/rinke/solutions/io/HeatShrinkDecoder.java">Java Implementation by Stefan Rinke</see>
	public class HeatShrinkDecoder
	{
		private const int NoBits = -1;

		private int _inputSize;        /* bytes in input buffer */
		private int _inputIndex;       /* offset to next unprocessed input byte */
		private int _outputCount;      /* how many bytes to output */
		private int _outputIndex;      /* index for bytes to output */
		private int _headIndex;        /* head of window buffer */
		private State _state;              /* current state machine node */
		private int _currentByte;       /* current byte of input */
		private int _bitIndex;          /* current bit index */

		/* Fields that are only used if dynamically allocated. */
		private readonly int _windowSize;         /* window buffer bits */
		private readonly int _lookaheadSize;      /* lookahead bits */
		private readonly int _inputBufferSize; /* input buffer size */

		/* Input buffer, then expansion window buffer */
		private byte[] _buffer;

		public HeatShrinkDecoder(int windowSize, int lookaheadSize, int inputBufferSize)
		{
			_windowSize = windowSize;
			_lookaheadSize = lookaheadSize;
			//int buffers_sz = (1 << windowSize) + input_buffer_size;
			_inputBufferSize = inputBufferSize;
			Reset();
		}

		public Result Finish()
		{
			switch (_state) {
				case State.HsdsTagBit:
					return _inputSize == 0 ? Result.res(Code.Done) : Result.res(Code.More);

				/*
				 * If we want to finish with no input, but are in these states, it's
				 * because the 0-bit padding to the last byte looks like a backref
				 * marker bit followed by all 0s for index and count bits.
				 */
				case State.HsdsBackrefIndexLsb:
				case State.HsdsBackrefIndexMsb:
				case State.HsdsBackrefCountLsb:
				case State.HsdsBackrefCountMsb:
					return _inputSize == 0 ? Result.res(Code.Done) : Result.res(Code.More);

				/*
				 * If the output stream is padded with 0xFFs (possibly due to being
				 * in flash memory), also explicitly check the input size rather
				 * than uselessly returning MORE but yielding 0 bytes when polling.
				 */
				case State.HsdsYieldLiteral:
					return _inputSize == 0 ? Result.res(Code.Done) : Result.res(Code.More);

				default:
					return Result.res(Code.More);
			}

		}

		public void Reset()
		{
			var bufSz = 1 << _windowSize;
			var inputSz = _inputBufferSize;
			_buffer = new byte[bufSz + inputSz];
			_state = State.HsdsTagBit;
			_inputSize = 0;
			_inputIndex = 0;
			_bitIndex = 0x00;
			_currentByte = 0x00;
			_outputCount = 0;
			_outputIndex = 0;
			_headIndex = 0;
		}

		public Result Sink(byte[] inBuffer, int offset, int size)
		{
			if (inBuffer == null) {
				throw new ArgumentException("inBuffer must not be null");
			}

			var rem = _inputBufferSize - _inputSize;
			if (rem == 0) {
				return Result.res(0, Code.Full);
			}

			// int size = in_buf.length;
			size = rem < size ? rem : size;
			/* copy into input buffer (at head of buffers) */
			//memcpy(&hsd->buffers[hsd->input_size], in_buf, size);
			Buffer.BlockCopy(inBuffer, offset, _buffer, _inputSize, size);
			_inputSize += size;
			return Result.res(size, Code.Ok);
		}

		public void Decode(Stream reader, Stream os)
		{
			var inbuffer = new byte[1 << _windowSize];
			var outbuffer = new byte[4 << _windowSize];
			var inputOffset = 0;
			var remainingInInput = 0;
			while (true) {
				Result res;
				do { // read and fill input buffer until full
					if (remainingInInput == 0) {
						// read some input bytes

						remainingInInput = reader.Read(inbuffer, 0, 4096);
						inputOffset = 0;
					}
					if (remainingInInput == 0) {
						res = Finish();
						break;
					}
					res = Sink(inbuffer, inputOffset, remainingInInput);
					if (res.IsError()) throw new Exception("error sink");
					remainingInInput -= res.Count;
					inputOffset += res.Count;
				} while (res.Code != Code.Full);

				if (res.Code == Code.Done) break;
				// now input buffer is full, poll for output
				do {
					res = Poll(outbuffer);
					if (res.IsError()) throw new Exception("error poll");
					if (res.Count > 0) {
						os.Write(outbuffer, 0, res.Count);
					}
				} while (res.Code == Code.More);
				//if( res.code == DONE ) break;
			}

		}
		
		/// <summary>
		/// poll decoded bytes into outBuffer.
		/// </summary>
		/// <param name="outBuffer">must not be null</param>
		/// <returns>count byte were polled</returns>
		public Result Poll(byte[] outBuffer /*, int offset , size_t *output_size*/)
		{
			if (outBuffer == null) {
				throw new ArgumentException("outbuffer must not be null");
			}

			var outBufSize = outBuffer.Length;

			var oi = new OutputInfo {
				Buf = outBuffer,
				BufSize = outBufSize,
				OutputSize = 0
			};

			while (true) {
				var inState = _state;
				switch (inState) {
					case State.HsdsTagBit:
						_state = TagBit();
						break;
					case State.HsdsYieldLiteral:
						_state = YieldLiteral(oi);
						break;
					case State.HsdsBackrefIndexMsb:
						_state = BackrefIndexMsb();
						break;
					case State.HsdsBackrefIndexLsb:
						_state = BackrefIndexLsb();
						break;
					case State.HsdsBackrefCountMsb:
						_state = BackrefCountMsb();
						break;
					case State.HsdsBackrefCountLsb:
						_state = BackrefCountLsb();
						break;
					case State.HsdsYieldBackref:
						_state = YieldBackref(oi);
						break;
					default:
						return Result.res(Code.ErrorUnknown);
				}

				/* If the current state cannot advance, check if input or output
				 * buffer are exhausted. */
				if (_state == inState) {
					if (oi.OutputSize == outBufSize)
						return Result.res(oi.OutputSize, Code.More);
					return Result.res(oi.OutputSize, Code.Empty);
				}
			}
		}

		private State YieldBackref(OutputInfo oi)
		{
			var count = oi.BufSize - oi.OutputSize;
			if (count > 0) {
				int i;
				if (_outputCount < count) count = _outputCount;
				//uint8_t *buf = &buffers[input_buffer_size];
				var mask = (1 << _windowSize) - 1;

				var negOffset = _outputIndex;

				//	        ASSERT(neg_offset <= mask + 1);
				//	        ASSERT(count <= (size_t)(1 << BACKREF_COUNT_BITS(hsd)));

				for (i = 0; i < count; i++) {
					var c = _buffer[_inputBufferSize + ((_headIndex - negOffset) & mask)];
					PushByte(oi, c);
					_buffer[_inputBufferSize + (_headIndex & mask)] = c;
					_headIndex++;
				}
				_outputCount -= count;
				if (_outputCount == 0) { return State.HsdsTagBit; }
			}
			return State.HsdsYieldBackref;
		}

		private void PushByte(OutputInfo oi, byte c)
		{
			//log.Debug(" -- pushing byte: 0x%02x ('%c')", c, isPrint(c) ? c : '.');
			oi.Buf[oi.OutputSize++] = c;
		}

		private State BackrefCountLsb()
		{
			var brBitCt = _lookaheadSize;
			var bits = GetBits(brBitCt < 8 ? brBitCt : 8);
			//log.debug("-- backref count (lsb), got 0x{} (+1)", bits);
			if (bits == NoBits) { return State.HsdsBackrefCountLsb; }
			_outputCount |= bits;
			_outputCount++;
			return State.HsdsYieldBackref;
		}

		/* Get the next COUNT bits from the input buffer, saving incremental progress.
		 * Returns NO_BITS on end of input, or if more than 15 bits are requested. */
		private int GetBits(int count)
		{
			var accumulator = 0;
			int i;
			if (count > 15) { return NoBits; }
			//log.debug("-- popping {} bit(s)", count);

			/* If we aren't able to get COUNT bits, suspend immediately, because we
			 * don't track how many bits of COUNT we've accumulated before suspend. */
			if (_inputSize == 0) {
				if (_bitIndex < (1 << (count - 1))) { return NoBits; }
			}

			for (i = 0; i < count; i++) {
				if (_bitIndex == 0x00) {
					if (_inputSize == 0) {
						//log.debug("  -- out of bits, suspending w/ accumulator of {} (0x{})",  accumulator, accumulator);
						return NoBits;
					}
					_currentByte = _buffer[_inputIndex++];
					//log.debug("  -- pulled byte 0x{}", currentByte);
					if (_inputIndex == _inputSize) {
						_inputIndex = 0; /* input is exhausted */
						_inputSize = 0;
					}
					_bitIndex = 0x80;
				}
				accumulator <<= 1;
				if ((_currentByte & _bitIndex) != 0) {
					accumulator |= 0x01;
					//	            if (0) {
					//	            	log.debug("  -- got 1, accumulator 0x%04x, bit_index 0x%02x\n",
					//	                accumulator, bit_index);
					//	            }
				} else {
					//	            if (0) {
					//	            	log.debug("  -- got 0, accumulator 0x%04x, bit_index 0x%02x\n",
					//	                accumulator, bit_index);
					//	            }
				}
				_bitIndex >>= 1;
			}

			if (count > 1) {
				//log.debug("  -- accumulated {}", accumulator);
			}
			return accumulator;
		}

		private State BackrefCountMsb()
		{
			var brBitCt = _lookaheadSize;
			Debug.Assert(brBitCt > 8);
			var bits = GetBits(brBitCt - 8);
			//log.debug("-- backref count (msb), got 0x{} (+1)", Integer.toHexString(bits));
			if (bits == NoBits) { return State.HsdsBackrefCountMsb; }
			_outputCount = bits << 8;
			return State.HsdsBackrefCountLsb;
		}

		private State BackrefIndexLsb()
		{
			var bitCt = _windowSize;
			var bits = GetBits(bitCt < 8 ? bitCt : 8);
			//log.debug("-- backref index (lsb), got 0x{} (+1)", Integer.toHexString(bits));
			if (bits == NoBits) { return State.HsdsBackrefIndexLsb; }
			_outputIndex |= bits;
			_outputIndex++;
			var brBitCt = _lookaheadSize;
			_outputCount = 0;
			return (brBitCt > 8) ? State.HsdsBackrefCountMsb : State.HsdsBackrefCountLsb;
		}

		private State BackrefIndexMsb()
		{
			var bitCt = _windowSize;
			Debug.Assert(bitCt > 8);
			var bits = GetBits(bitCt - 8);
			//log.debug("-- backref index (msb), got 0x{} (+1)", Integer.toHexString(bits));
			if (bits == NoBits) { return State.HsdsBackrefIndexMsb; }
			_outputIndex = bits << 8;
			return State.HsdsBackrefIndexLsb;
		}

		private State YieldLiteral(OutputInfo oi)
		{
			/* Emit a repeated section from the window buffer, and add it (again)
			 * to the window buffer. (Note that the repetition can include
			 * itself.)*/
			if (oi.OutputSize < oi.BufSize) {
				var b = GetBits(8);
				if (b == NoBits) { return State.HsdsYieldLiteral; } /* out of input */

				//uint8_t *buf = &hsd->buffers[input_buffer_size];
				var mask = (1 << _windowSize) - 1;
				var c = (byte)(b & 0xFF);
				//log.debug("-- emitting literal byte 0x{} ('{}')\n", c, isPrint(c) ? c : '.');
				_buffer[_inputBufferSize + (_headIndex++ & mask)] = c;
				PushByte(oi, c);
				return State.HsdsTagBit;
			} else {
				return State.HsdsYieldLiteral;
			}
		}

		private State TagBit()
		{
			var bits = GetBits(1);  // get tag bit
			if (bits == NoBits) {
				return State.HsdsTagBit;
			} else if (bits != 0) {
				return State.HsdsYieldLiteral;
			} else if (_windowSize > 8) {
				return State.HsdsBackrefIndexMsb;
			} else {
				_outputIndex = 0;
				return State.HsdsBackrefIndexLsb;
			}
		}

		enum State
		{
			/// <summary>
			/// tag bit
			/// </summary>
			HsdsTagBit,
			/// <summary>
			/// ready to yield literal byte
			/// </summary>
			HsdsYieldLiteral,
			/// <summary>
			/// most significant byte of index
			/// </summary>
			HsdsBackrefIndexMsb,
			/// <summary>
			/// least significant byte of index
			/// </summary>
			HsdsBackrefIndexLsb,
			/// <summary>
			/// most significant byte of count
			/// </summary>
			HsdsBackrefCountMsb,
			/// <summary>
			/// least significant byte of count
			/// </summary>
			HsdsBackrefCountLsb,
			/// <summary>
			/// ready to yield back-reference
			/// </summary>
			HsdsYieldBackref
		};
	}
}
