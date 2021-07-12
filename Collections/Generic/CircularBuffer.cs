using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace exoLib.Collections.Generic
{
	/// <summary>
	/// First-in, first-out collection with automatic overwrite support.
	/// </summary>
	public class CircularBuffer<T> : ICollection<T>, ICollection
	{
		/// <summary>
		/// Internal data storage.
		/// </summary>
		private T[] _buffer;
		/// <summary>
		/// Current capacity.
		/// </summary>
		private int _capacity;
		/// <summary>
		/// Used to synchronize access.
		/// </summary>
		[NonSerialized]
		private object _syncObject;
		/// <summary>
		/// Creates new empty circular buffer with provided capacity.
		/// <param name="capacity">Maximum capacity.</param>
		/// </summary>
		public CircularBuffer(int capacity) : this(capacity, true)
		{
		}
		/// <summary>
		/// Creates new empty circular buffer with provided capacity.
		/// <param name="capacity">Maximum capacity.</param>
		/// <param name="allowOverwrite">Whether oldest elements should be overwritten by newer ones if max capacity is reached.</param>
		/// </summary>
		public CircularBuffer(int capacity, bool allowOverwrite)
		{
			if (capacity < 0)
				throw new ArgumentException("The buffer capacity must be greater than or equal to zero.", nameof(capacity));

			Count = 0;
			Head = 0;
			Tail = 0;
			Capacity = capacity;
			AllowOverwrite = allowOverwrite;
			_buffer = new T[capacity];
		}
		/// <summary>
		/// Gets or sets whether oldest elements should be overwritten by newer ones if max capacity is reached.
		/// </summary>
		public bool AllowOverwrite { get; set; }
		/// <summary>
		/// Gets or sets the current capacity.
		/// Value must be greater or equal than current buffer size.
		/// </summary>
		public int Capacity
		{
			get
			{
				return _capacity;
			}
			set
			{
				if (value != _capacity)
				{
					if (value < this.Count)
						throw new ArgumentOutOfRangeException(nameof(value), value, "The new capacity must be greater than or equal to the buffer size.");

					T[] buffer = new T[value];
					if (this.Count > 0)
						this.CopyTo(buffer);

					_buffer = buffer;
					_capacity = value;
				}
			}
		}
		/// <summary>
		/// Returns index of the first object in the collection.
		/// </summary>
		public int Head { get; protected set; }
		/// <summary>
		/// Returns index of the last object in the collection.
		/// </summary>
		public int Tail { get; protected set; }
		/// <summary>
		/// Returns true when the buffer is empty.
		/// </summary>
		public bool IsEmpty
		{
			get => this.Count == 0;
		}
		/// <summary>
		/// Returns true when the buffer is at max capacity.
		/// </summary>
		public virtual bool IsFull
		{
			get => !AllowOverwrite && Count == Capacity;
		}
		/// <summary>
		/// Returns number of elements in the collection.
		/// </summary>
		public int Count { get; protected set; }
		/// <summary>
		/// Copies provided number of items to provided array starting at given index.
		/// </summary>
		/// <param name="index">Index to start copy to.</param>
		/// <param name="array">Array to copy to.</param>
		/// <param name="arrayIndex">Index at which the copying starts.</param>
		/// <param name="count">The number of elements to copy.</param>
		public virtual void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			if (count > Count)
				throw new ArgumentOutOfRangeException(nameof(count), count, "The read count cannot be greater than the buffer size.");

			var bufferIndex = index;
			for (int i = 0; i < count; i++, bufferIndex++, arrayIndex++)
			{
				if (bufferIndex == Capacity)
					bufferIndex = 0;

				array[arrayIndex] = _buffer[bufferIndex];
			}
		}
		/// <summary>
		/// Removes provided number of items from the collection and returns them.
		/// </summary>
		public T[] Get(int count)
		{
			var result = new T[count];
			Get(result);
			return result;
		}
		/// <summary>
		/// Copies items from this collection to fill provided array.
		/// </summary>
		public int Get(T[] array)
		{
			return Get(array, 0, array.Length);
		}
		/// <summary>
		/// Copies and removes provided number of elements.
		/// </summary>
		/// <param name="array">Destination array.</param>
		/// <param name="arrayIndex">Copy start index.</param>
		/// <param name="count">Number of elements.</param>
		public int Get(T[] array, int arrayIndex, int count)
		{
			var realCount = Math.Min(count, this.Count);
			for (int i = 0; i < realCount; i++, this.Head++, arrayIndex++)
			{
				if (Head == Capacity)
					Head = 0;

				array[arrayIndex] = _buffer[Head];
			}

			Count -= realCount;
			return realCount;
		}
		/// <summary>
		/// Removes and returns item from the beginning of the collection.
		/// </summary>
		public virtual T Get()
		{
			if (IsEmpty)
				throw new InvalidOperationException("The buffer is empty.");

			var item = _buffer[Head];
			if (++Head == Capacity)
				Head = 0;

			Count--;
			return item;
		}
		/// <summary>
		/// Returns an enumerator for this collection.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			var bufferIndex = Head;
			for (int i = 0; i < Count; i++, bufferIndex++)
			{
				if (bufferIndex == Capacity)
					bufferIndex = 0;

				yield return _buffer[bufferIndex];
			}
		}
		/// <summary>
		/// Removes and returns the item from the end of this collection.
		/// </summary>
		public virtual T GetLast()
		{
			if (IsEmpty)
				throw new InvalidOperationException("The buffer is empty.");

			int index = this.GetTailIndex();
			var item = _buffer[index];

			if (--Tail < 0)
				Tail = 0;

			Count--;
			return item;
		}
		/// <summary>
		/// Returns first object without removing it.
		/// </summary>
		public virtual T Peek()
		{
			if (IsEmpty)
				throw new InvalidOperationException("The buffer is empty.");

			return _buffer[Head];
		}
		/// <summary>
		/// Returns item at provided index (starting at current head) without its removal.
		/// </summary>
		public T PeekAt(int index)
		{
			if (IsEmpty)
				throw new InvalidOperationException("The buffer is empty.");

			index = Head + index;
			if (index >= Capacity)
				index -= Capacity;

			return _buffer[index];
		}
		/// <summary>
		/// Returns provided number of items without their removal.
		/// </summary>
		public T[] Peek(int count)
		{
			if (IsEmpty)
				throw new InvalidOperationException("The buffer is empty.");

			var items = new T[count];
			CopyTo(items);
			return items;
		}
		/// <summary>
		/// Returns the item at the end of the collection without its removal.
		/// </summary>
		public T PeekLast()
		{
			if (IsEmpty)
				throw new InvalidOperationException("The buffer is empty.");

			int index = GetTailIndex();
			return _buffer[index];
		}
		/// <summary>
		/// Copies provided array into this collection.
		/// </summary>
		public int Add(T[] array)
		{
			return Add(array, 0, array.Length);
		}
		/// <summary>
		/// Copies a range of items into this collection.
		/// </summary>
		/// <param name="array">Array to copy from.</param>
		/// <param name="arrayIndex">Index at which to start copying.</param>
		/// <param name="count">The number of elements to copy.</param>
		public int Add(T[] array, int arrayIndex, int count)
		{
			if (!AllowOverwrite && count > Capacity - Count)
				throw new InvalidOperationException("The buffer is not large enough!");

			int i;
			for (i = 0; i < count; i++)
				Add(array[arrayIndex + i]);

			return i;
		}
		/// <summary>
		/// Adds an object to the of the collection.
		/// </summary>
		/// <param name="item">Item to add.</param>
		public void Add(T item)
		{
			if (!AllowOverwrite && Count == Capacity)
				throw new InvalidOperationException("The buffer does not have sufficient capacity to put new items.");

			_buffer[Tail++] = item;
			if (Count == Capacity)
			{
				if (++Head >= Capacity)
					Head -= Capacity;
			}

			if (Tail == Capacity)
				Tail = 0;

			if (Count != Capacity)
				Count++;
		}
		/// <summary>
		/// Moves the starting index.
		/// </summary>
		public void Skip(int count)
		{
			Head += count;
			if (Head >= Capacity)
				Head -= Capacity;
		}
		/// <summary>
		/// Returns current index of the collection end.
		/// </summary>
		private int GetTailIndex()
		{
			if (Tail == 0)
				return Count - 1;
			else
				return Tail - 1;
		}
		/// <summary>
		/// Returns the number of elements in this collection.
		/// </summary>
		int ICollection.Count => Count;
		/// <summary>
		/// Returns whether this collection is thread safe. (it is not)
		/// </summary>
		bool ICollection.IsSynchronized => false;
		/// <summary>
		/// Allows synchronyzing access.
		/// </summary>
		object ICollection.SyncRoot
		{
			get
			{
				if (_syncObject == null)
				{
					Interlocked.CompareExchange<object>(ref _syncObject, new object(), null);
				}

				return _syncObject;
			}
		}
		/// <summary>
		/// Returns the number of elements in this collection.
		int ICollection<T>.Count => Count;
		/// <summary>
		/// Returns whether this collection is readonly.
		/// </summary>
		bool ICollection<T>.IsReadOnly => false;
		/// <summary>
		/// Removes all items from the collection.
		/// </summary>
		public void Clear()
		{
			Count = 0;
			Head = 0;
			Tail = 0;
			_buffer = new T[this.Capacity];
		}
		/// <summary>
		/// Return true if specified item is contained in this collection.
		/// </summary>
		/// <param name="item">Item to find./>.</param>
		public bool Contains(T item)
		{
			int bufferIndex;
			EqualityComparer<T> comparer;
			bool result;

			bufferIndex = this.Head;
			comparer = EqualityComparer<T>.Default;
			result = false;

			for (int i = 0; i < this.Count; i++, bufferIndex++)
			{
				if (bufferIndex == this.Capacity)
					bufferIndex = 0;

				if (item == null && _buffer[bufferIndex] == null || _buffer[bufferIndex] != null && comparer.Equals(_buffer[bufferIndex], item))
				{
					result = true;
					break;
				}
			}

			return result;
		}
		/// <summary>
		/// Copies the entire collection to an array.
		/// </summary>
		public void CopyTo(T[] array)
		{
			CopyTo(array, 0);
		}
		/// <summary>
		/// Copies the buffer into an array.
		/// </summary>
		/// <param name="array">Array to copy to.</param>
		/// <param name="index">Index of where to copy from.</param>
		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			CopyTo((T[])array, arrayIndex);
		}
		/// <summary>
		/// Copies the buffer into an array.
		/// </summary>
		/// <param name="array">Array to copy to.</param>
		/// <param name="index">Index of where to copy from.</param>
		public void CopyTo(T[] array, int index)
		{
			CopyTo(Head, array, index, Math.Min(Count, array.Length - index));
		}
		/// <summary>
		/// Copies the elements of the collection into a new array.
		/// </summary>
		public T[] ToArray()
		{
			var result = new T[Count];
			CopyTo(result);
			return result;
		}
		/// <summary>
		/// Adds an item to the collection.
		/// </summary>
		void ICollection<T>.Add(T item)
		{
			Add(item);
		}
		/// <summary>
		/// Returns an enumerator for this collection.
		/// </summary>
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		/// <summary>
		/// Returns an enumerator for this collection.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		/// <summary>
		/// Removing objects is disallowed.
		/// </summary>
		bool ICollection<T>.Remove(T item)
		{
			throw new NotSupportedException("This collection does not support removal of items.");
		}
	}
}