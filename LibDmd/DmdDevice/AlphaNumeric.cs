using System;
using NLog;

namespace LibDmd.DmdDevice
{
	public class AlphaNumeric
	{

		static readonly byte[] FrameBuffer = new byte[4096];
		static readonly byte[] BlankBuffer = new byte[4096];

		static readonly byte[,] SegSizes = {
			{5,5,5,5,5,5,2,2,5,5,5,2,5,5,5,1},
			{5,5,5,5,5,5,5,2,0,0,0,0,0,0,0,0},
			{5,5,5,5,5,5,5,2,5,5,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
			{5,2,2,5,2,2,5,2,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
		};

		static readonly byte[,,,] Segs = {	

			// Alphanumeric display characters
			{
				{{1,0},{2,0},{3,0},{4,0},{5,0}}, // 0 top
				{{6,0},{6,1},{6,2},{6,3},{6,4}}, // 1 right top
				{{6,6},{6,7},{6,8},{6,9},{6,10}}, // 2 right bottom
				{{1,10},{2,10},{3,10},{4,10},{5,10}}, // 3 bottom
				{{0,6},{0,7},{0,8},{0,9},{0,10}}, // 4 left bottom
				{{0,0},{0,1},{0,2},{0,3},{0,4}}, // 5 left top
				{{1,5},{2,5},{0,0},{0,0},{0,0}}, // 6 middle left
				{{7,9},{7,10},{0,0},{0,0},{0,0}}, // 7 comma
				{{0,0},{1,1},{1,2},{2,3},{2,4}}, // 8 diag top left
				{{3,0},{3,1},{3,2},{3,3},{3,4}}, // 9 center top
				{{6,0},{5,1},{5,2},{4,3},{4,4}}, // 10 diag top right
				{{4,5},{5,5},{0,0},{0,0},{0,0}}, // 11 middle right
				{{4,6},{4,7},{5,8},{5,9},{6,10}}, // 12 diag bottom right
				{{3,6},{3,7},{3,8},{3,9},{3,10}}, // 13 center bottom
				{{0,10},{2,6},{2,7},{1,8},{1,9}}, // 14 diag bottom left
				{{7,10},{0,0},{0,0},{0,0},{0,0}} // 15 period
			},

			// 8 segment LED characters
			{
				{{1,0},{2,0},{3,0},{4,0},{5,0}}, // 0 top
				{{6,0},{6,1},{6,2},{6,3},{6,4}}, // 1 right top
				{{6,6},{6,7},{6,8},{6,9},{6,10}}, // 2 right bottom
				{{1,10},{2,10},{3,10},{4,10},{5,10}}, // 3 bottom
				{{0,6},{0,7},{0,8},{0,9},{0,10}}, // 4 left bottom
				{{0,0},{0,1},{0,2},{0,3},{0,4}}, // 5 left top
				{{1,5},{2,5},{3,5},{4,5},{5,5}}, // 6 middle
				{{7,9},{7,10},{0,0},{0,0},{0,0}}, // 7 comma
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}}
			},
			// 10 segment LED characters
			{
				{{1,0},{2,0},{3,0},{4,0},{5,0}}, // 0 top
				{{6,0},{6,1},{6,2},{6,3},{6,4}}, // 1 right top
				{{6,6},{6,7},{6,8},{6,9},{6,10}}, // 2 right bottom
				{{1,10},{2,10},{3,10},{4,10},{5,10}}, // 3 bottom
				{{0,6},{0,7},{0,8},{0,9},{0,10}}, // 4 left bottom
				{{0,0},{0,1},{0,2},{0,3},{0,4}}, // 5 left top
				{{1,5},{2,5},{3,5},{4,5},{5,5}}, // 6 middle
				{{7,9},{7,10},{0,0},{0,0},{0,0}}, // 7 comma
				{{3,0},{3,1},{3,2},{3,3},{3,4}}, // 8 diag top
				{{3,6},{3,7},{3,8},{3,9},{3,10}}, // 9 diag bottom
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}}
			},
			// alphanumeric display characters (reversed comma with period)
			{
				{{1,0},{2,0},{3,0},{4,0},{5,0}},
				{{6,0},{6,1},{6,2},{6,3},{6,4}},
				{{6,6},{6,7},{6,8},{6,9},{6,10}},
				{{1,10},{2,10},{3,10},{4,10},{5,10}},
				{{0,6},{0,7},{0,8},{0,9},{0,10}},
				{{0,0},{0,1},{0,2},{0,3},{0,4}},
				{{1,5},{2,5},{0,0},{0,0},{0,0}},
				{{7,9},{7,10},{0,0},{0,0},{0,0}},
				{{0,0},{1,1},{1,2},{2,3},{2,4}},
				{{3,0},{3,1},{3,2},{3,3},{3,4}},
				{{6,0},{5,1},{5,2},{4,3},{4,4}},
				{{4,5},{5,5},{0,0},{0,0},{0,0}},
				{{4,6},{4,7},{5,8},{5,9},{6,10}},
				{{3,6},{3,7},{3,8},{3,9},{3,10}},
				{{0,10},{2,6},{2,7},{1,8},{1,9}},
				{{7,10},{0,0},{0,0},{0,0},{0,0}}
			}, 
			// 8 segment LED characters with dots instead of commas
			{
				{{1,0},{2,0},{3,0},{4,0},{5,0}},
				{{6,0},{6,1},{6,2},{6,3},{6,4}},
				{{6,6},{6,7},{6,8},{6,9},{6,10}},
				{{1,10},{2,10},{3,10},{4,10},{5,10}},
				{{0,6},{0,7},{0,8},{0,9},{0,10}},
				{{0,0},{0,1},{0,2},{0,3},{0,4}},
				{{1,5},{2,5},{3,5},{4,5},{5,5}},
				{{7,9},{7,10},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}}
			},

			// 8 segment LED characters SMALL
			{
				{{1,0},{2,0},{3,0},{4,0},{5,0}}, // 0 top
				{{6,1},{6,2},{0,0},{0,0},{0,0}}, // 1 top right
				{{6,4},{6,5},{0,0},{0,0},{0,0}}, // 2 bottom right
				{{1,6},{2,6},{3,6},{4,6},{5,6}}, // 3 bottom
				{{0,4},{0,5},{0,0},{0,0},{0,0}}, // 4 bottom left
				{{0,1},{0,2},{0,0},{0,0},{0,0}}, // 5 top left
				{{1,3},{2,3},{3,3},{4,3},{5,3}}, // 6 middle
				{{7,5},{7,6},{0,0},{0,0},{0,0}}, // 7 commy
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}}
			},
			// 10 segment LED characters SMALL
			{
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}},
				{{0,0},{0,0},{0,0},{0,0},{0,0}}
			}
		};

		static void SmoothDigitCorners(int x, int y)
		{
			if (GetPixel(x, 1 + y) && GetPixel(1 + x, y))
				DrawPixel(0 + x, y, 0);
			if (GetPixel(x + 6, 1 + y) && GetPixel(5 + x, y))
				DrawPixel(6 + x, y, 0);
			if (GetPixel(x, 9 + y) && GetPixel(1 + x, 10 + y))
				DrawPixel(0 + x, 10 + y, 0);
			if (GetPixel(x + 6, 9 + y) && GetPixel(5 + x, 10 + y))
				DrawPixel(6 + x, 10 + y, 0);
		}

		static void DrawSegment(int x, int y, byte type, ushort seg, byte colour)
		{
			int i;
			for (i = 0; i < SegSizes[type, seg]; i++) {
				DrawPixel(Segs[type, seg, i, 0] + x, Segs[type, seg, i, 1] + y, colour);
			}
		}

		static bool GetPixel(int x, int y)
		{
			return FrameBuffer[y * 128 + x] > 0;
		}

		static void DrawPixel(int x, int y, byte colour)
		{
			FrameBuffer[y * 128 + x] = colour;
		}

		static void Clear()
		{
			Buffer.BlockCopy(BlankBuffer, 0, FrameBuffer, 0, FrameBuffer.Length);
		}

		public static byte[] Render2x16Alpha(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 16; i++) {
				for (j = 0; j < 16; j++) {
					if (((seg_data[i] >> j) & 0x1) > 0)
						DrawSegment(i * 8, 2, 0, j, 3);
					if (((seg_data[i + 16] >> j) & 0x1) > 0)
						DrawSegment(i * 8, 19, 0, j, 3);
					SmoothDigitCorners(i * 8, 2);
					SmoothDigitCorners(i * 8, 19);
				}
			}
			return FrameBuffer;
		}

		protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static byte[] Render2x20Alpha(ushort[] seg_data)
		{
			//Logger.Info(string.Join(", ", seg_data.ToList().Select(seg => seg.ToString("X"))));
			Clear();
			byte i, j;
			for (i = 0; i < 16; i++) {
				for (j = 0; j < 16; j++) {
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment(i * 8, 2, 0, j, 3);
					if (((seg_data[i + 20] >> j) & 0x1) != 0)
						DrawSegment(i * 8, 19, 0, j, 3);
					SmoothDigitCorners(i * 8, 2);
					SmoothDigitCorners(i * 8, 19);
				}
			}
			return FrameBuffer;
		}

		public static byte[] Render2x7Alpha_2x7Num(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 14; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 alphanumeric
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 2, 0, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 2);
					// 2x7 numeric
					if (((seg_data[i + 14] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 19, 1, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 19);
				}
			}
			return FrameBuffer;
		}

		public static byte[] Render2x7Alpha_2x7Num_4x1Num(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 14; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 alphanumeric
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 0, 0, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 0);
					// 2x7 numeric
					if (((seg_data[i + 14] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 21, 1, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 21);
				}
			}
			// 4x1 numeric small
			for (j = 0; j < 16; j++) {
				if (((seg_data[28] >> j) & 0x1) != 0)
					DrawSegment(8, 12, 5, j, 3);
				if (((seg_data[29] >> j) & 0x1) != 0)
					DrawSegment(16, 12, 5, j, 3);
				if (((seg_data[30] >> j) & 0x1) != 0)
					DrawSegment(32, 12, 5, j, 3);
				if (((seg_data[31] >> j) & 0x1) != 0)
					DrawSegment(40, 12, 5, j, 3);
			}
			return FrameBuffer;
		}

		public static byte[] Render2x6Num_2x6Num_4x1Num(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 12; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 numeric
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 6) ? 0 : 4)) * 8, 0, 1, j, 3);
					SmoothDigitCorners((i + ((i < 6) ? 0 : 4)) * 8, 0);
					// 2x7 numeric
					if (((seg_data[i + 12] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 6) ? 0 : 4)) * 8, 12, 1, j, 3);
					SmoothDigitCorners((i + ((i < 6) ? 0 : 4)) * 8, 12);
				}
			}
			// 4x1 numeric small
			for (j = 0; j < 16; j++) {
				if (((seg_data[24] >> j) & 0x1) != 0)
					DrawSegment(8, 24, 5, j, 3);
				if (((seg_data[25] >> j) & 0x1) != 0)
					DrawSegment(16, 24, 5, j, 3);
				if (((seg_data[26] >> j) & 0x1) != 0)
					DrawSegment(32, 24, 5, j, 3);
				if (((seg_data[27] >> j) & 0x1) != 0)
					DrawSegment(40, 24, 5, j, 3);
			}
			return FrameBuffer;
		}

		public static byte[] Render2x6Num10_2x6Num10_4x1Num(ushort[] seg_data)
		{
			byte i, j;
			for (i = 0; i < 12; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 numeric
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 6) ? 0 : 4)) * 8, 0, 2, j, 3);
					SmoothDigitCorners((i + ((i < 6) ? 0 : 4)) * 8, 0);
					// 2x7 numeric
					if (((seg_data[i + 12] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 6) ? 0 : 4)) * 8, 20, 2, j, 3);
					SmoothDigitCorners((i + ((i < 6) ? 0 : 4)) * 8, 20);
				}
			}
			// 4x1 numeric small
			for (j = 0; j < 16; j++) {
				if (((seg_data[24] >> j) & 0x1) != 0)
					DrawSegment(8, 12, 5, j, 3);
				if (((seg_data[25] >> j) & 0x1) != 0)
					DrawSegment(16, 12, 5, j, 3);
				if (((seg_data[26] >> j) & 0x1) != 0)
					DrawSegment(32, 12, 5, j, 3);
				if (((seg_data[27] >> j) & 0x1) != 0)
					DrawSegment(40, 12, 5, j, 3);
			}
			return FrameBuffer;
		}

		public static byte[] Render2x7Num_2x7Num_4x1Num(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 14; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 numeric
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 0, 1, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 0);
					// 2x7 numeric
					if (((seg_data[i + 14] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 12, 1, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 12);
				}
			}
			// 4x1 numeric small
			for (j = 0; j < 16; j++) {
				if (((seg_data[28] >> j) & 0x1) != 0)
					DrawSegment(16, 24, 5, j, 3);
				if (((seg_data[29] >> j) & 0x1) != 0)
					DrawSegment(24, 24, 5, j, 3);
				if (((seg_data[30] >> j) & 0x1) != 0)
					DrawSegment(40, 24, 5, j, 3);
				if (((seg_data[31] >> j) & 0x1) != 0)
					DrawSegment(48, 24, 5, j, 3);
			}
			return FrameBuffer;
		}

		public static byte[] Render2x7Num_2x7Num_10x1Num(ushort[] seg_data, ushort[] extra_seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 14; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 numeric
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 0, 1, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 0);
					// 2x7 numeric
					if (((seg_data[i + 14] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 12, 1, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 12);
				}
			}
			// 10x1 numeric small
			for (j = 0; j < 16; j++) {
				if (((seg_data[28] >> j) & 0x1) != 0)
					DrawSegment(16, 24, 5, j, 3);
				if (((seg_data[29] >> j) & 0x1) != 0)
					DrawSegment(24, 24, 5, j, 3);
				if (((seg_data[30] >> j) & 0x1) != 0)
					DrawSegment(40, 24, 5, j, 3);
				if (((seg_data[31] >> j) & 0x1) != 0)
					DrawSegment(48, 24, 5, j, 3);
				if (((extra_seg_data[0] >> j) & 0x1) != 0)
					DrawSegment(64, 24, 5, j, 3);
				if (((extra_seg_data[1] >> j) & 0x1) != 0)
					DrawSegment(72, 24, 5, j, 3);
				if (((extra_seg_data[2] >> j) & 0x1) != 0)
					DrawSegment(88, 24, 5, j, 3);
				if (((extra_seg_data[3] >> j) & 0x1) != 0)
					DrawSegment(96, 24, 5, j, 3);
				if (((extra_seg_data[4] >> j) & 0x1) != 0)
					DrawSegment(112, 24, 5, j, 3);
				if (((extra_seg_data[5] >> j) & 0x1) != 0)
					DrawSegment(120, 24, 5, j, 3);
			}
			return FrameBuffer;
		}

		public static byte[] Render2x7Num_2x7Num_4x1Num_gen7(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 14; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 numeric
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 21, 1, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 21);
					// 2x7 numeric
					if (((seg_data[i + 14] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 1, 1, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 1);
				}
			}
			// 4x1 numeric small
			for (j = 0; j < 16; j++) {
				if (((seg_data[28] >> j) & 0x1) != 0)
					DrawSegment(8, 13, 5, j, 3);
				if (((seg_data[29] >> j) & 0x1) != 0)
					DrawSegment(16, 13, 5, j, 3);
				if (((seg_data[30] >> j) & 0x1) != 0)
					DrawSegment(32, 13, 5, j, 3);
				if (((seg_data[31] >> j) & 0x1) != 0)
					DrawSegment(40, 13, 5, j, 3);
			}
			return FrameBuffer;
		}

		public static byte[] Render2x7Num10_2x7Num10_4x1Num(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			Clear();
			for (i = 0; i < 14; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 numeric
					if (((seg_data[i] >> j) & 0x1) > 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 0, 2, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 0);
					// 2x7 numeric
					if (((seg_data[i + 14] >> j) & 0x1) > 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 20, 2, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 20);
				}
			}
			// 4x1 numeric small
			for (j = 0; j < 16; j++) {
				if (((seg_data[28] >> j) & 0x1) > 0)
					DrawSegment(8, 12, 5, j, 3);
				if (((seg_data[29] >> j) & 0x1) > 0)
					DrawSegment(16, 12, 5, j, 3);
				if (((seg_data[30] >> j) & 0x1) > 0)
					DrawSegment(32, 12, 5, j, 3);
				if (((seg_data[31] >> j) & 0x1) > 0)
					DrawSegment(40, 12, 5, j, 3);
			}
			return FrameBuffer;
		}

		public static byte[] Render4x7Num10(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 14; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 numeric10
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 1, 2, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 1);
					// 2x7 numeric10
					if (((seg_data[i + 14] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 13, 2, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 13);
				}
			}
			return FrameBuffer;
		}

		public static byte[] Render6x4Num_4x1Num(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 8; i++) {
				for (j = 0; j < 16; j++) {
					// 2x4 numeric
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 4) ? 0 : 2)) * 8, 1, 5, j, 3);
					SmoothDigitCorners((i + ((i < 4) ? 0 : 2)) * 8, 1);
					// 2x4 numeric
					if (((seg_data[i + 8] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 4) ? 0 : 2)) * 8, 9, 5, j, 3);
					SmoothDigitCorners((i + ((i < 4) ? 0 : 2)) * 8, 1);
					// 2x4 numeric
					if (((seg_data[i + 16] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 4) ? 0 : 2)) * 8, 17, 5, j, 3);
					SmoothDigitCorners((i + ((i < 4) ? 0 : 2)) * 8, 1);
				}
			}
			// 4x1 numeric small
			for (j = 0; j < 16; j++) {
				if (((seg_data[24] >> j) & 0x1) != 0)
					DrawSegment(16, 25, 5, j, 3);
				if (((seg_data[25] >> j) & 0x1) != 0)
					DrawSegment(24, 25, 5, j, 3);
				if (((seg_data[26] >> j) & 0x1) != 0)
					DrawSegment(48, 25, 5, j, 3);
				if (((seg_data[27] >> j) & 0x1) != 0)
					DrawSegment(56, 25, 5, j, 3);
			}
			return FrameBuffer;
		}

		public static byte[] Render2x7Num_4x1Num_1x16Alpha(ushort[] seg_data)
		{
			Clear();
			byte i, j;
			for (i = 0; i < 14; i++) {
				for (j = 0; j < 16; j++) {
					// 2x7 numeric
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i + ((i < 7) ? 0 : 2)) * 8, 0, 1, j, 3);
					SmoothDigitCorners((i + ((i < 7) ? 0 : 2)) * 8, 0);
				}
			}
			// 4x1 numeric small
			for (j = 0; j < 16; j++) {
				if (((seg_data[14] >> j) & 0x1) != 0)
					DrawSegment(16, 12, 5, j, 3);
				if (((seg_data[15] >> j) & 0x1) != 0)
					DrawSegment(24, 12, 5, j, 3);
				if (((seg_data[16] >> j) & 0x1) != 0)
					DrawSegment(40, 12, 5, j, 3);
				if (((seg_data[17] >> j) & 0x1) != 0)
					DrawSegment(48, 12, 5, j, 3);
			}
			// 1x16 alphanumeric
			for (i = 0; i < 12; i++) {
				for (j = 0; j < 16; j++) {
					if (((seg_data[i + 18] >> j) & 0x1) != 0)
						DrawSegment((i * 8) + 16, 21, 0, j, 3);
					SmoothDigitCorners((i * 8) + 16, 21);
				}
			}
			return FrameBuffer;
		}

		public static byte[] Render1x16Alpha_1x16Num_1x7Num(ushort[] seg_data)
		{
			Clear();
			byte i, j;

			// 1x16 alphanumeric
			for (i = 0; i < 16; i++) {
				for (j = 0; j < 16; j++) {
					if (((seg_data[i] >> j) & 0x1) != 0)
						DrawSegment((i * 8), 1, 0, j, 3);
					SmoothDigitCorners((i * 8), 1);
				}
			}

			// 1x16 numeric
			for (i = 0; i < 16; i++) {
				for (j = 0; j < 16; j++) {
					if (((seg_data[i + 16] >> j) & 0x1) != 0)
						DrawSegment((i * 8), 21, 1, j, 3);
					SmoothDigitCorners((i * 8), 21);
				}
			}

			// 1x7 numeric small
			for (i = 0; i < 7; i++) {
				for (j = 0; j < 16; j++) {
					if (((seg_data[i + 32] >> j) & 0x1) != 0)
						DrawSegment(i * 8, 13, 5, j, 3);
				}
			}
			return FrameBuffer;
		}
	}

	public enum NumericalLayout
		{
			None,
			__2x16Alpha,
			__2x20Alpha,
			__2x7Alpha_2x7Num,
			__2x7Alpha_2x7Num_4x1Num,
			__2x7Num_2x7Num_4x1Num,
			__2x7Num_2x7Num_10x1Num,
			__2x7Num_2x7Num_4x1Num_gen7,
			__2x7Num10_2x7Num10_4x1Num,
			__2x6Num_2x6Num_4x1Num,
			__2x6Num10_2x6Num10_4x1Num,
			__4x7Num10,
			__6x4Num_4x1Num,
			__2x7Num_4x1Num_1x16Alpha,
			__1x16Alpha_1x16Num_1x7Num
		}
}
