using System;
using System.Collections;
using System.Collections.Generic;

// Class used to maintain a list with a finite capacity.  Items are removed in a FIFO order when the list is at max capacity.
public class RollingList<T> : IEnumerable<T>
{
	private readonly T[] itemArray;
	private int startIndex;
	private int endIndex;
	private int numStoredItems;
	private readonly int maximumCount;

	public RollingList(int maximumCount)
	{
		if (maximumCount <= 0)
		{
			throw new ArgumentException(null, "maximumCount size must be greater than zero");
		}

		this.maximumCount = maximumCount;
		itemArray = new T[maximumCount];
		numStoredItems = 0;
		startIndex = 0;
		endIndex = 0;
	}

	public int Count
	{
		get { return numStoredItems; }
	}

	public void Clear()
	{
		startIndex = 0;
		endIndex = 0;
		numStoredItems = 0;
	}

	public void Add(T value)
	{
		if (numStoredItems == maximumCount)
		{
			startIndex++;
			startIndex = startIndex % maximumCount;
		}
		else
		{
			++numStoredItems;
		}

		itemArray[endIndex] = value;
		++endIndex;
		endIndex = endIndex % maximumCount;

	}

	public T this[int index]
	{
		get
		{
			if (index < 0 || index >= numStoredItems)
				throw new ArgumentOutOfRangeException();

			int translatedIndex = (startIndex + index) % maximumCount;
			return itemArray[translatedIndex];
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (int i = 0; i < numStoredItems; ++i)
		{
			yield return this[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
