using System;
using LibDmd.Output;

namespace LibDmd.Frame
{
	public abstract class BaseFrame
	{
		public Dimensions Dimensions { get; protected set; }

		protected Dimensions GetTargetDimensions(IFixedSizeDestination fixedDest, IMultiSizeDestination multiDest)
		{
			if (fixedDest == null && multiDest == null) {
				return Dimensions.Dynamic;
			}

			if (fixedDest != null) {
				return fixedDest.FixedSize;
			}

			var nextLargerDim = Dimensions.Dynamic;
			var maxDim = Dimensions.Dynamic;
			foreach (var multiDim in multiDest.Sizes) {
				if (Dimensions == multiDim) {
					return multiDim;
				}

				if (Dimensions < multiDim && multiDim > nextLargerDim) {
					nextLargerDim = multiDim;
				}

				if (multiDim > maxDim) {
					maxDim = multiDim;
				}
			}

			return nextLargerDim == Dimensions.Dynamic ? maxDim : nextLargerDim;
		}

		protected byte[] CenterFrame(Dimensions targetDim, byte[] data, int bytesPerPixel)
		{
			var padX = (targetDim.Width - Dimensions.Width) / 2;
			var padY = (targetDim.Height - Dimensions.Height) / 2;
			var frameData = new byte[targetDim.Surface * bytesPerPixel];
			var ySrc = 0;
			for (var yDest = padY; yDest < padY + Dimensions.Height; yDest++) {
				Buffer.BlockCopy(
					data,
					ySrc * Dimensions.Width * bytesPerPixel,
					frameData,
					(yDest * targetDim.Width + padX) * bytesPerPixel,
					Dimensions.Width * bytesPerPixel
				);
				ySrc++;
			}

			return frameData;
		}

		protected byte[] CenterVertically(Dimensions targetDim, byte[] data, int bytesPerPixel)
		{
			var padY = (targetDim.Height - Dimensions.Height) / 2;
			var transformedFrameData = new byte[targetDim.Surface * bytesPerPixel];
			Buffer.BlockCopy(
				data,
				0,
				transformedFrameData,
				padY * targetDim.Width * bytesPerPixel,
				data.Length
			);
			return transformedFrameData;
		}

	}
}
