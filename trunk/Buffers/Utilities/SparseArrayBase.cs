using System;
using System.Collections.Generic;

namespace Buffers.Utilities
{
	abstract class SparseArrayBase
	{
		public static readonly int HighPartLength = 1 << 14;
		public static readonly int LowPartLength = 1 << 18;				

		protected SparseArrayBase()
		{
			LowerBound = uint.MaxValue;
			UpperBound = uint.MinValue;
		}

		public uint LowerBound { get; private set; }
		public uint UpperBound { get; private set; }

		protected void _CheckArgument(int high, int low)
		{
			if (high < 0 || high >= HighPartLength)
				throw new IndexOutOfRangeException("high");
			if (low < 0 || low >= LowPartLength)
				throw new IndexOutOfRangeException("low");
		}
		protected void _CalcRange(uint index, int count,
			out int fromhigh, out int fromlow, out int tohigh, out int tolow)
		{
			fromhigh = (int)(index / (uint)LowPartLength);
			fromlow = (int)(index % LowPartLength);

			index += (uint)count;
			tohigh = (int)(index / (uint)LowPartLength);
			tolow = (int)(index % LowPartLength);
		}
		protected void _UpdateBounds(uint index)
		{
			LowerBound = Math.Min(LowerBound, index);
			UpperBound = Math.Max(UpperBound, index);
		}
		protected void _UpdateBounds(int high, int low)
		{
			uint index = (uint)high * (uint)LowPartLength + (uint)low;
			_UpdateBounds(index);
		}
	}
}