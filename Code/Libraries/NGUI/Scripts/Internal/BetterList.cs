//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// This improved version of the System.Collections.Generic.List that doesn't release the buffer on Clear(), resulting in better performance and less garbage collection.
/// </summary>

public class BetterList<T>
{
#if UNITY_FLASH

	List<T> mList = new List<T>();
	
	/// <summary>
	/// Direct access to the buffer. Note that you should not use its 'Length' parameter, but instead use BetterList.size.
	/// </summary>
	
	public T this[int i]
	{
		get { return mList[i]; }
		set { mList[i] = value; }
	}
	
	/// <summary>
	/// Compatibility with the non-flash syntax.
	/// </summary>
	
	public List<T> buffer { get { return mList; } }

	/// <summary>
	/// Direct access to the buffer's size. Note that it's only public for speed and efficiency. You shouldn't modify it.
	/// </summary>

	public int size { get { return mList.Count; } }

	/// <summary>
	/// For 'foreach' functionality.
	/// </summary>

	public IEnumerator<T> GetEnumerator () { return mList.GetEnumerator(); }

	/// <summary>
	/// Clear the array by resetting its size to zero. Note that the memory is not actually released.
	/// </summary>

	public void Clear () { mList.Clear(); }

	/// <summary>
	/// Clear the array and release the used memory.
	/// </summary>

	public void Release () { mList.Clear(); }

	/// <summary>
	/// Add the specified item to the end of the list.
	/// </summary>

	public void Add (T item) { mList.Add(item); }

	/// <summary>
	/// Insert an item at the specified index, pushing the entries back.
	/// </summary>

	public void Insert (int index, T item) { mList.Insert(index, item); }

	/// <summary>
	/// Returns 'true' if the specified item is within the list.
	/// </summary>

	public bool Contains (T item) { return mList.Contains(item); }

	/// <summary>
	/// Remove the specified item from the list. Note that RemoveAt() is faster and is advisable if you already know the index.
	/// </summary>

	public bool Remove (T item) { return mList.Remove(item); }

	/// <summary>
	/// Remove an item at the specified index.
	/// </summary>

	public void RemoveAt (int index) { mList.RemoveAt(index); }

	/// <summary>
	/// Remove an item from the end.
	/// </summary>

	public T Pop ()
	{
		if (buffer != null && size != 0)
		{
			T val = buffer[mList.Count - 1];
			mList.RemoveAt(mList.Count - 1);
			return val;
		}
		return default(T);
	}

	/// <summary>
	/// Mimic List's ToArray() functionality, except that in this case the list is resized to match the current size.
	/// </summary>

	public T[] ToArray () { return mList.ToArray(); }

	/// <summary>
	/// List.Sort equivalent.
	/// </summary>

	public void Sort (System.Comparison<T> comparer) { mList.Sort(comparer); }

#else

	/// <summary>
	/// Direct access to the buffer. Note that you should not use its 'Length' parameter, but instead use BetterList.size.
	/// </summary>

	public T[] buffer;

	/// <summary>
	/// Direct access to the buffer's size. Note that it's only public for speed and efficiency. You shouldn't modify it.
	/// </summary>

	public int size = 0;
	
	public int Capacity()
	{
		if( buffer != null )
			return buffer.Length;
		
		return 0;
	}
	/// <summary>
	/// For 'foreach' functionality.
	/// </summary>

	public IEnumerator<T> GetEnumerator ()
	{
		if (buffer != null)
		{
			for (int i = 0; i < size; ++i)
			{
				yield return buffer[i];
			}
		}
	}
	
	/// <summary>
	/// Convenience function. I recommend using .buffer instead.
	/// </summary>
	
	public T this[int i]
	{
		get { return buffer[i]; }
		set { buffer[i] = value; }
	}

	/// <summary>
	/// Helper function that expands the size of the array, maintaining the content.
	/// </summary>

	void AllocateMore ( int newsize = 0)
	{
        if(newsize<32) 
            newsize=32;
        if(buffer!=null)
            newsize = Math.Max(buffer.Length << 1,newsize);
        
        if(newsize>64000)
        {
            UnityEngine.Debug.Log("BetterList.AllocateMore - invalid count:" + newsize);
        }
        
        Array.Resize( ref buffer, newsize);
	}

	/// <summary>
	/// Trim the unnecessary memory, resizing the buffer to be of 'Length' size.
	/// Call this function only if you are sure that the buffer won't need to resize anytime soon.
	/// </summary>

	void Trim ()
	{
		
		if (size > 0)
		{
			if (size < buffer.Length)
			{
                Array.Resize(ref buffer , size );
			}
		}
		else buffer = null;

	}

	/// <summary>
	/// Clear the array by resetting its size to zero. Note that the memory is not actually released.
	/// </summary>

	public void Clear () { size = 0; }

	/// <summary>
	/// Clear the array and release the used memory.
	/// </summary>

	public void Release () { size = 0; buffer = null; }

	/// <summary>
	/// Add the specified item to the end of the list.
	/// </summary>

	public void Add (T item)
	{
		if (buffer == null || size == buffer.Length) AllocateMore();
		buffer[size++] = item;
	}

	/// <summary>
	/// Insert an item at the specified index, pushing the entries back.
	/// </summary>
    public void AddCount( int count )
    {
        if(count<0 || count>64000)
        {
			UnityEngine.Debug.Log("BetterList.AddCount - invalid count:" + count);
        }
        
        int newsize = size+count;
        if(buffer==null || newsize > buffer.Length)
           AllocateMore(newsize);  
        size = newsize;
    }
    
    public void ReserveCount( int count )
    {
        //same as AddCount, but do not update the size:
        if(count<0 || count>64000)
        {
			UnityEngine.Debug.Log("BetterList.AddCount - invalid count:" + count);
        }
        
        int newsize = size+count;
        if(buffer==null || newsize > buffer.Length)
           AllocateMore(newsize);  
    }
    

	public void Insert (int index, T item)
	{
		if (buffer == null || size == buffer.Length) AllocateMore();

		if (index < size)
		{
			for (int i = size; i > index; --i) buffer[i] = buffer[i - 1];
			buffer[index] = item;
			++size;
		}
		else Add(item);
	}

	/// <summary>
	/// Returns 'true' if the specified item is within the list.
	/// </summary>

	public bool Contains (T item)
	{
		if (buffer == null) return false;
		for (int i = 0; i < size; ++i) if (buffer[i].Equals(item)) return true;
		return false;
	}

	/// <summary>
	/// Remove the specified item from the list. Note that RemoveAt() is faster and is advisable if you already know the index.
	/// </summary>

	public bool Remove (T item)
	{
		if (buffer != null)
		{
			EqualityComparer<T> comp = EqualityComparer<T>.Default;

			for (int i = 0; i < size; ++i)
			{
				if (comp.Equals(buffer[i], item))
				{
					--size;
					buffer[i] = default(T);
					for (int b = i; b < size; ++b) buffer[b] = buffer[b + 1];
					buffer[size] = default(T);
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Remove an item at the specified index.
	/// </summary>

	public void RemoveAt (int index)
	{
		if (buffer != null && index < size)
		{
			--size;
			buffer[index] = default(T);
			for (int b = index; b < size; ++b) buffer[b] = buffer[b + 1];
			buffer[size] = default(T);
		}
	}

	/// <summary>
	/// Remove an item from the end.
	/// </summary>

	public T Pop ()
	{
		if (buffer != null && size != 0)
		{
			T val = buffer[--size];
			buffer[size] = default(T);
			return val;
		}
		return default(T);
	}

	/// <summary>
	/// Mimic List's ToArray() functionality, except that in this case the list is resized to match the current size.
	/// </summary>

	public T[] ToArray () { 	

		Trim();
		
		return buffer; 
	}

#if ENABLE_DRAW_OPTIMIZATION	
	
	/// <summary>
	/// Mimic List's ToArray() functionality, except that in this case the list is not resized but all elements wiht index > size are set to default(T) if reset is true.
	/// </summary>

	public T[] ToArrayNoResize ( bool reset ) {	 

		if( reset  )
		{
			if (size > 0)
			{
				if ( size < buffer.Length && size < oldSize )
				{
					for(int i = size ; i < oldSize; ++ i )
					{
						buffer[i] = default(T);
					}					
				}
				
			}
		}		
		
		return buffer; 
	}

	public T[] GetBuffer()
	{
		return buffer;
	}
#endif
	
	
	/// <summary>
	/// List.Sort equivalent.
	/// </summary>

	public void Sort (System.Comparison<T> comparer)
	{
		int start = 0;
		int max = size - 1;
		bool changed = true;

		while (changed)
		{
			changed = false;

			for (int i = start; i < max; ++i)
			{
				if (comparer.Invoke(buffer[i], buffer[i+1]) > 0)
				{
					T temp = buffer[i];
					buffer[i] = buffer[i + 1];
					buffer[i + 1] = temp;
					changed = true;
				}
				else if (!changed)
				{
					// Nothing has changed -- we can start here next time
					start = i - 1;
				}
			}
			if(start<0) 
				start = 0;
		}
	}

#endif
}
