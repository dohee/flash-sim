using System;
using System.Collections.Generic;

namespace Buffers.Utilities
{
	sealed class SparseArray<T> : SparseArrayBase, IEnumerable<T>
	{
		private T[][] array = new T[HighPartLength][];

		private T _GetNoCheck(int high, int low)
		{
			return array[high] == null ? default(T) : array[high][low];
		}
		private void _SetNoCheck(int high, int low, T value)
		{
			_UpdateBounds(high, low);

			if (value == null || value.Equals(default(T)))
				return;

			if (array[high] == null)
				array[high] = new T[LowPartLength];

			array[high][low] = value;
		}
		private void _GetWithinOne(T[] buffer, int offset, int high, int lowstart, int count)
		{
			if (array[high] == null)
			{
				for (int i = 0; i < count; i++)
					buffer[offset++] = default(T);
			}
			else
			{
				T[] subarray = array[high];
				for (int i = 0; i < count; i++)
					buffer[offset++] = subarray[lowstart++];
			}
		}
		private void _SetWithinOne(T[] buffer, int offset, int high, int lowstart, int count)
		{
			_UpdateBounds(high, lowstart);
			_UpdateBounds(high, lowstart + count - 1);
			int i;

			for (i = 0; i < count; i++, lowstart++, offset++)
			{
				T value = buffer[offset];

				if (value != null && !value.Equals(default(T)))
					break;
			}

			if (i != count)
			{
				if (array[high] == null)
					array[high] = new T[LowPartLength];

				T[] subarray = array[high];

				for (; i < count; i++)
					subarray[lowstart++] = buffer[offset++];
			}
		}

		public T this[int high, int low]
		{
			get
			{
				_CheckArgument(high, low);
				return _GetNoCheck(high, low);
			}
			set
			{
				_CheckArgument(high, low);
				_SetNoCheck(high, low, value);
			}
		}
		public T this[uint index]
		{
			get
			{
				return _GetNoCheck((int)(index / (uint)LowPartLength),
					(int)(index % LowPartLength));
			}
			set
			{
				_SetNoCheck((int)(index / (uint)LowPartLength),
					(int)(index % LowPartLength), value);
			}
		}

		public T[] GetArray(int high)
		{
			_CheckArgument(high, 0);
			return array[high];
		}
		public void GetBlock(T[] buffer, int offset, uint index, int count)
		{
			int fromhigh, fromlow, tohigh, tolow;
			_CalcRange(index, count, out fromhigh, out fromlow, out tohigh, out tolow);

			if (fromhigh == tohigh)
			{
				_GetWithinOne(buffer, offset, fromhigh, fromlow, tolow - fromlow);
			}
			else
			{
				_GetWithinOne(buffer, offset, fromhigh++, fromlow, LowPartLength - fromlow);
				offset += (LowPartLength - fromlow);

				while (fromhigh < tohigh)
				{
					_GetWithinOne(buffer, offset, fromhigh++, 0, LowPartLength);
					offset += LowPartLength;
				}

				_GetWithinOne(buffer, offset, fromhigh, 0, tolow);
			}
		}
		public void SetBlock(T[] buffer, int offset, uint index, int count)
		{
			int fromhigh, fromlow, tohigh, tolow;
			_CalcRange(index, count, out fromhigh, out fromlow, out tohigh, out tolow);

			if (fromhigh == tohigh)
			{
				_SetWithinOne(buffer, offset, fromhigh, fromlow, tolow - fromlow);
			}
			else
			{
				_SetWithinOne(buffer, offset, fromhigh++, fromlow, LowPartLength - fromlow);
				offset += (LowPartLength - fromlow);

				while (fromhigh < tohigh)
				{
					_SetWithinOne(buffer, offset, fromhigh++, 0, LowPartLength);
					offset += LowPartLength;
				}

				_SetWithinOne(buffer, offset, fromhigh, 0, tolow);
			}
		}


		#region IEnumerable<T> 成员
		public IEnumerator<T> GetEnumerator()
		{
			for (uint i = LowerBound; i <= UpperBound; i++)
				yield return this[i];
		}
		#endregion

		#region IEnumerable 成员
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion	}
	}
}