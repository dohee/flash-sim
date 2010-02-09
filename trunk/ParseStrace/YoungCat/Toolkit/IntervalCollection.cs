using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace YoungCat.Toolkit
{
	public struct Interval<T>
	{
		public T Start { get; private set; }
		public T End { get; private set; }

		public Interval(T start, T end)
			: this()
		{
			Start = start;
			End = end;
		}
	}


	public class IntervalCollection<TKey, TValue> :
		IDictionary<Interval<TKey>, TValue>, IDictionary
		where TKey : IComparable<TKey>
	{
		private readonly SortedList<TKey, MapValueType> map =
			new SortedList<TKey, MapValueType>();

		private struct MapValueType
		{
			public readonly TValue Value;
			public readonly bool IsEnd;

			public MapValueType(TValue value, bool isEnd)
			{
				Value = value;
				IsEnd = isEnd;
			}
		}



		public void Add(TKey start, TKey end, TValue value)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			map.Clear();
			Count = 0;
		}

		public int Count
		{
			get;
			private set;
		}

		public bool ContainsKey(Interval<TKey> key)
		{
			return IndexOfKey(key) >= 0;
		}
		public bool ContainsPoint(TKey point)
		{
			return IndexOfPoint(point) >= 0;
		}

		private int IndexOfKey(Interval<TKey> key)
		{
			int index = map.IndexOfKey(key.Start);

			if (index > 0 &&
				map.Count > index + 1 &&
				!map.Values[index].IsEnd &&
				map.Keys[index + 1].CompareTo(key.End) == 0)
				return index;
			else
				return -1;
		}
		private int IndexOfPoint(TKey point)
		{
			int index = map.BinarySearch(point);

			if (index < 0)
				index = (~index) - 1;

			if (map.Count > index + 1 &&
				!map.Values[index].IsEnd &&
				map.Keys[index + 1].CompareTo(point) > 0)
				return index;
			else
				return -1;
		}

		public KeyValuePair<Interval<TKey>, TValue> this[TKey point]
		{
			get
			{
				KeyValuePair<Interval<TKey>, TValue> entry;

				if (TryGetEntry(point, out entry))
					return entry;
				else
					throw new KeyNotFoundException();
			}
		}
		public TValue this[Interval<TKey> key]
		{
			get
			{
				TValue value;

				if (TryGetValue(key, out value))
					return value;
				else
					throw new KeyNotFoundException();
			}
			set
			{
				int index = IndexOfKey(key);

				if (index >= 0)
					map.Values[index] = new MapValueType(value, map.Values[index].IsEnd);
				else
					Add(key.Start, key.End, value);
			}
		}

		public bool Remove(TKey exactStart, TKey exactEnd)
		{
			throw new NotImplementedException();
		}
		public bool Remove(Interval<TKey> key)
		{
			return Remove(key.Start, key.End);
		}

		public bool Subtract(TKey start, TKey end)
		{
			throw new NotImplementedException();
		}

		public bool TryGetEntry(TKey point, out KeyValuePair<Interval<TKey>, TValue> entry)
		{
			int index = IndexOfPoint(point);

			if (index >= 0)
			{
				entry = new KeyValuePair<Interval<TKey>, TValue>(
					new Interval<TKey>(map.Keys[index], map.Keys[index + 1]),
					map.Values[index].Value);
				return true;
			}
			else
			{
				entry = default(KeyValuePair<Interval<TKey>, TValue>);
				return false;
			}
		}
		public bool TryGetValue(Interval<TKey> key, out TValue value)
		{
			int index = IndexOfKey(key);

			if (index >= 0)
			{
				value = map.Values[index].Value;
				return true;
			}
			else
			{
				value = default(TValue);
				return false;
			}
		}

		public List<Interval<TKey>> Keys
		{
			get { return null; }
		}
		public List<TValue> Values
		{
			get
			{
				return null;
			}
		}
		public IEnumerator<KeyValuePair<Interval<TKey>, TValue>> GetEnumerator()
		{
			throw new NotImplementedException();
		}


		#region 显式实现接口
		void IDictionary<Interval<TKey>, TValue>.
		Add(Interval<TKey> key, TValue value)
		{
			Add(key.Start, key.End, value);
		}
		void ICollection<KeyValuePair<Interval<TKey>, TValue>>.
		Add(KeyValuePair<Interval<TKey>, TValue> item)
		{
			Add(item.Key.Start, item.Key.End, item.Value);
		}
		void IDictionary.
		Add(object key, object value)
		{
			Interval<TKey> key2 = (Interval<TKey>)key;
			Add(key2.Start, key2.End, (TValue)value);
		}

		bool IDictionary.
		Contains(object key)
		{
			return ContainsKey((Interval<TKey>)key);
		}
		bool ICollection<KeyValuePair<Interval<TKey>, TValue>>.
		Contains(KeyValuePair<Interval<TKey>, TValue> item)
		{
			int index = IndexOfKey(item.Key);
			return (index >= 0 && map.Values[index].Equals(item.Value));
		}

		void ICollection<KeyValuePair<Interval<TKey>, TValue>>.
		CopyTo(KeyValuePair<Interval<TKey>, TValue>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}
		void ICollection.
		CopyTo(Array array, int index)
		{
			(this as ICollection<KeyValuePair<Interval<TKey>, TValue>>).
				CopyTo((KeyValuePair<Interval<TKey>, TValue>[])array, index);
		}

		IEnumerator IEnumerable.
		GetEnumerator()
		{
			return GetEnumerator();
		}
		IDictionaryEnumerator IDictionary.
		GetEnumerator()
		{
			throw new NotImplementedException();
		}

		bool IDictionary.
		IsFixedSize
		{
			get { return false; }
		}

		bool ICollection<KeyValuePair<Interval<TKey>, TValue>>.
		IsReadOnly
		{
			get { return false; }
		}
		bool IDictionary.
		IsReadOnly
		{
			get { return false; }
		}

		bool ICollection.
		IsSynchronized
		{
			get { return false; }
		}

		ICollection<Interval<TKey>> IDictionary<Interval<TKey>, TValue>.
		Keys
		{
			get { return Keys; }
		}
		ICollection IDictionary.
		Keys
		{
			get { return Keys; }
		}

		bool ICollection<KeyValuePair<Interval<TKey>, TValue>>.
		Remove(KeyValuePair<Interval<TKey>, TValue> item)
		{
			throw new NotImplementedException();
		}
		void IDictionary.
		Remove(object key)
		{
			throw new NotImplementedException();
		}

		object IDictionary.
		this[object key]
		{
			get
			{
				return this[(Interval<TKey>)key];
			}
			set
			{
				this[(Interval<TKey>)key] = (TValue)value;
			}
		}

		object ICollection.
		SyncRoot
		{
			get { throw new NotSupportedException(); }
		}

		ICollection<TValue> IDictionary<Interval<TKey>, TValue>.
		Values
		{
			get { return Values; }
		}
		ICollection IDictionary.
		Values
		{
			get { return Values; }
		}
		#endregion
	}
}
